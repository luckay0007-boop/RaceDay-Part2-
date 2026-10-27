using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class EnrolmentService : IEnrolmentService
{
    private readonly RaceDayDbContext _db;

    public EnrolmentService(RaceDayDbContext db) => _db = db;

    public async Task<EnrolmentResponseDto> CreateEnrolmentAsync(int participantId, CreateEnrolmentDto dto)
    {
        // Verify the category exists
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId)
            ?? throw new KeyNotFoundException($"Category with ID {dto.CategoryId} not found.");

        // Prevent double-registration (same participant + same category)
        var alreadyEnrolled = await _db.Enrolments
            .AnyAsync(e => e.ParticipantId == participantId && e.CategoryId == dto.CategoryId);

        if (alreadyEnrolled)
            throw new InvalidOperationException("You are already registered for this category.");

        // Check capacity
        if (category.MaxParticipants.HasValue)
        {
            var currentCount = await _db.Enrolments
                .CountAsync(e => e.CategoryId == dto.CategoryId && e.Status != "Withdrawn");

            if (currentCount >= category.MaxParticipants.Value)
                throw new InvalidOperationException("This category has reached maximum capacity.");
        }

        // Generate unique bib number: event-type prefix + random number
        var bibNumber = await GenerateBibNumberAsync(category);

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = dto.CategoryId,
            BibNumber = bibNumber,
            Status = "Registered",
            EnrolmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        // Reload navigation properties for response
        await _db.Entry(enrolment).Reference(e => e.Participant).LoadAsync();
        await _db.Entry(enrolment).Reference(e => e.Category).LoadAsync();
        await _db.Entry(enrolment.Category).Reference(c => c.Event).LoadAsync();

        return MapToDto(enrolment);
    }

    public async Task<List<EnrolmentResponseDto>> GetEnrolmentsByUserAsync(int userId)
    {
        return await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
                .ThenInclude(c => c.Event)
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => MapToDto(e))
            .ToListAsync();
    }

    private async Task<string> GenerateBibNumberAsync(Category category)
    {
        // Create a prefix based on event name initials
        var eventName = category.Event.Name;
        var prefix = string.Concat(eventName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(w => char.ToUpper(w[0])));

        // Find the next available number
        var existingBibs = await _db.Enrolments
            .Where(e => e.CategoryId == category.CategoryId && e.BibNumber != null)
            .Select(e => e.BibNumber!)
            .ToListAsync();

        var maxNumber = existingBibs
            .Select(b =>
            {
                var parts = b.Split('-');
                return parts.Length > 1 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}-{(maxNumber + 1).ToString("D4")}";
    }

    private static EnrolmentResponseDto MapToDto(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant is not null ? $"{e.Participant.FirstName} {e.Participant.LastName}" : string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        EventName = e.Category?.Event?.Name ?? string.Empty,
        EnrolmentDate = e.EnrolmentDate,
        BibNumber = e.BibNumber,
        Status = e.Status
    };
}
