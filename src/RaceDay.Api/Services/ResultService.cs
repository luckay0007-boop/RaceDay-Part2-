using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Results;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class ResultService : IResultService
{
    private readonly RaceDayDbContext _db;

    public ResultService(RaceDayDbContext db) => _db = db;

    public async Task<ResultResponseDto> SubmitResultAsync(SubmitResultDto dto)
    {
        // Validate status
        var validStatuses = new[] { "Finished", "DNF", "DSQ" };
        if (!validStatuses.Contains(dto.Status))
            throw new ArgumentException($"Invalid result status '{dto.Status}'. Must be Finished, DNF, or DSQ.");

        // Verify the enrolment exists
        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId)
            ?? throw new KeyNotFoundException($"Enrolment with ID {dto.EnrolmentId} not found.");

        // Check if result already exists for this enrolment
        var existingResult = await _db.Results
            .AnyAsync(r => r.EnrolmentId == dto.EnrolmentId);

        if (existingResult)
            throw new InvalidOperationException("A result has already been recorded for this enrolment.");

        // Validate: finished results should have a finish time
        if (dto.Status == "Finished" && dto.FinishTime is null)
            throw new ArgumentException("FinishTime is required when status is 'Finished'.");

        var result = new Result
        {
            EnrolmentId = dto.EnrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            Status = dto.Status,
            RecordedAt = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return new ResultResponseDto
        {
            ResultId = result.ResultId,
            EnrolmentId = result.EnrolmentId,
            ParticipantName = $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}",
            BibNumber = enrolment.BibNumber,
            CategoryName = enrolment.Category.Name,
            FinishTime = result.FinishTime,
            Position = result.Position,
            Status = result.Status,
            RecordedAt = result.RecordedAt
        };
    }

    public async Task<List<ResultResponseDto>> GetResultsByEventAsync(int eventId)
    {
        return await _db.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Enrolment.Category.Name)
            .ThenBy(r => r.Position)
            .Select(r => new ResultResponseDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                BibNumber = r.Enrolment.BibNumber,
                CategoryName = r.Enrolment.Category.Name,
                FinishTime = r.FinishTime,
                Position = r.Position,
                Status = r.Status,
                RecordedAt = r.RecordedAt
            })
            .ToListAsync();
    }
}
