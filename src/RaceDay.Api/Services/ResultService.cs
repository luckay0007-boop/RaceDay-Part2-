using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Results;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class ResultService : IResultService
{
    private static readonly string[] ValidStatuses = { "Finished", "DNF", "DSQ" };

    private readonly RaceDayDbContext _db;

    public ResultService(RaceDayDbContext db) => _db = db;

    public async Task<ResultResponseDto> SubmitResultAsync(int organiserId, SubmitResultDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            throw new ArgumentException($"Invalid result status '{dto.Status}'. Must be Finished, DNF, or DSQ.");

        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId)
            ?? throw new KeyNotFoundException($"Enrolment with ID {dto.EnrolmentId} not found.");

        if (enrolment.Category.Event.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only record results for your own events.");

        if (dto.Status == "Finished" && dto.FinishTime is null)
            throw new ArgumentException("FinishTime is required when status is 'Finished'.");

        var finishTime = dto.Status == "Finished" ? dto.FinishTime : null;
        var position = dto.Status == "Finished" ? dto.Position : null;

        var result = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentId == dto.EnrolmentId);
        if (result is null)
        {
            result = new Result
            {
                EnrolmentId = dto.EnrolmentId,
                FinishTime = finishTime,
                Position = position,
                Status = dto.Status,
                RecordedAt = DateTime.UtcNow
            };
            _db.Results.Add(result);
        }
        else
        {
            result.FinishTime = finishTime;
            result.Position = position;
            result.Status = dto.Status;
            result.RecordedAt = DateTime.UtcNow;
        }

        if (enrolment.Status is "Pending" or "Registered")
            enrolment.Status = "Confirmed";

        await _db.SaveChangesAsync();

        var totalFinishers = await _db.Results
            .CountAsync(r => r.Enrolment.CategoryId == enrolment.CategoryId && r.Status == "Finished");

        return MapToDto(result, enrolment, totalFinishers);
    }

    public async Task<List<ResultResponseDto>> GetResultsByEventAsync(int eventId)
    {
        var results = await _db.Results
            .AsNoTracking()
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
                    .ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Enrolment.Category.Distance)
            .ThenBy(r => r.Position ?? int.MaxValue)
            .ThenBy(r => r.FinishTime)
            .ToListAsync();

        var finishersByCategory = results
            .Where(r => r.Status == "Finished")
            .GroupBy(r => r.Enrolment.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        return results
            .Select(r => MapToDto(
                r,
                r.Enrolment,
                finishersByCategory.GetValueOrDefault(r.Enrolment.CategoryId, 0)))
            .ToList();
    }

    public async Task<List<ResultResponseDto>> GetResultsByParticipantAsync(int participantId)
    {
        var results = await _db.Results
            .AsNoTracking()
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
                    .ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        var categoryIds = results.Select(r => r.Enrolment.CategoryId).Distinct().ToList();
        var finishersByCategory = await _db.Results
            .AsNoTracking()
            .Where(r => categoryIds.Contains(r.Enrolment.CategoryId) && r.Status == "Finished")
            .GroupBy(r => r.Enrolment.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return results
            .Select(r => MapToDto(
                r,
                r.Enrolment,
                finishersByCategory.GetValueOrDefault(r.Enrolment.CategoryId, 0)))
            .ToList();
    }

    private static ResultResponseDto MapToDto(Result r, Enrolment e, int totalFinishers)
    {
        var ev = e.Category.Event;
        var posDisplay = r.Status == "Finished" && r.Position.HasValue
            ? $"{FormatOrdinal(r.Position.Value)} out of {Math.Max(totalFinishers, r.Position.Value)} {(Math.Max(totalFinishers, r.Position.Value) == 1 ? "finisher" : "finishers")}"
            : r.Status;

        return new ResultResponseDto
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            EventId = ev?.EventId ?? e.Category.EventId,
            EventName = ev?.Name ?? string.Empty,
            EventDate = ev?.EventDate ?? default,
            EventLocation = ev?.Location ?? string.Empty,
            EventCity = ev?.City ?? string.Empty,
            EventType = ev?.EventType ?? string.Empty,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            CategoryDistance = e.Category.Distance,
            ParticipantName = $"{e.Participant.FirstName} {e.Participant.LastName}",
            BibNumber = e.BibNumber,
            FinishTime = r.FinishTime,
            Position = r.Position,
            TotalFinishers = totalFinishers,
            PositionDisplay = posDisplay,
            Status = r.Status,
            RecordedAt = r.RecordedAt
        };
    }

    public static string FormatOrdinal(int number)
    {
        if (number <= 0) return number.ToString();
        var rem100 = number % 100;
        if (rem100 is 11 or 12 or 13) return $"{number}th";
        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }
}
