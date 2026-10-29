using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class EnrolmentService : IEnrolmentService
{
    private static readonly string[] ValidStatuses = { "Pending", "Registered", "Confirmed", "Withdrawn", "DNS" };

    private readonly RaceDayDbContext _db;

    public EnrolmentService(RaceDayDbContext db) => _db = db;

    public async Task<EnrolmentResponseDto> CreateEnrolmentAsync(int participantId, CreateEnrolmentDto dto)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId)
            ?? throw new KeyNotFoundException($"Category with ID {dto.CategoryId} not found.");

        if (category.Event.Status == "Cancelled")
            throw new InvalidOperationException("Cannot enrol in a cancelled event.");

        var alreadyEnrolled = await _db.Enrolments
            .AnyAsync(e => e.ParticipantId == participantId && e.CategoryId == dto.CategoryId);

        if (alreadyEnrolled)
            throw new InvalidOperationException("You are already enrolled in this category.");

        if (category.MaxParticipants.HasValue)
        {
            var currentCount = await _db.Enrolments
                .CountAsync(e => e.CategoryId == dto.CategoryId && e.Status != "Withdrawn");

            if (currentCount >= category.MaxParticipants.Value)
                throw new InvalidOperationException("This category has reached maximum capacity.");
        }

        var bibNumber = await GenerateBibNumberAsync(category);

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = dto.CategoryId,
            BibNumber = bibNumber,
            Status = "Confirmed",
            EnrolmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        await _db.Entry(enrolment).Reference(e => e.Participant).LoadAsync();
        await _db.Entry(enrolment).Reference(e => e.Category).LoadAsync();
        await _db.Entry(enrolment.Category).Reference(c => c.Event).LoadAsync();

        return MapToDto(enrolment);
    }

    public async Task<List<EnrolmentResponseDto>> GetEnrolmentsByUserAsync(int userId)
    {
        var list = await _db.Enrolments
            .AsNoTracking()
            .Include(e => e.Participant)
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<List<EnrolmentResponseDto>> GetEnrolmentsByEventAsync(int eventId, int organiserId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.");

        if (ev.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only view enrolments for your own events.");

        var list = await _db.Enrolments
            .AsNoTracking()
            .Include(e => e.Participant)
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.Category.Distance)
            .ThenBy(e => e.BibNumber)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<EnrolmentResponseDto?> UpdateEnrolmentStatusAsync(int enrolmentId, int organiserId, string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException($"Invalid enrolment status '{status}'. Must be Confirmed, Pending, Registered, Withdrawn, or DNS.");

        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null) return null;

        if (enrolment.Category.Event.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only update enrolments for your own events.");

        enrolment.Status = status;
        await _db.SaveChangesAsync();

        return MapToDto(enrolment);
    }

    private async Task<string> GenerateBibNumberAsync(Category category)
    {
        var eventName = category.Event.Name;
        var prefix = string.Concat(eventName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Where(w => char.IsLetterOrDigit(w[0]))
            .Select(w => char.ToUpperInvariant(w[0])));

        if (string.IsNullOrEmpty(prefix))
            prefix = "RD";

        var existingBibs = await _db.Enrolments
            .Where(e => e.Category.EventId == category.EventId && e.BibNumber != null)
            .Select(e => e.BibNumber!)
            .ToListAsync();

        var maxNumber = existingBibs
            .Select(b =>
            {
                var parts = b.Split('-');
                return parts.Length > 1 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            .DefaultIfEmpty(100)
            .Max();

        return $"{prefix}-{(maxNumber + 1):D4}";
    }

    private static EnrolmentResponseDto MapToDto(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant is not null ? $"{e.Participant.FirstName} {e.Participant.LastName}" : string.Empty,
        ParticipantEmail = e.Participant?.Email ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        CategoryDistance = e.Category?.Distance ?? 0m,
        EntryFee = e.Category?.EntryFee ?? 0m,
        EventId = e.Category?.EventId ?? 0,
        EventName = e.Category?.Event?.Name ?? string.Empty,
        EventDate = e.Category?.Event?.EventDate ?? default,
        EventStartTime = e.Category?.Event?.StartTime ?? default,
        EventLocation = e.Category?.Event?.Location ?? string.Empty,
        EventCity = e.Category?.Event?.City ?? string.Empty,
        EventType = e.Category?.Event?.EventType ?? string.Empty,
        EventStatus = e.Category?.Event?.Status ?? string.Empty,
        EnrolmentDate = e.EnrolmentDate,
        BibNumber = e.BibNumber,
        Status = e.Status
    };
}
