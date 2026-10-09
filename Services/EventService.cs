using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class EventService : IEventService
{
    private readonly RaceDayDbContext _db;

    public EventService(RaceDayDbContext db) => _db = db;

    public async Task<List<EventResponseDto>> GetAllEventsAsync()
    {
        return await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .OrderByDescending(e => e.EventDate)
            .Select(e => MapToDto(e))
            .ToListAsync();
    }

    public async Task<EventResponseDto?> GetEventByIdAsync(int eventId)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        return ev is null ? null : MapToDto(ev);
    }

    public async Task<EventResponseDto> CreateEventAsync(int organiserId, CreateEventDto dto)
    {
        // Validate EventType
        var validTypes = new[] { "Running", "Walking", "Cycling" };
        if (!validTypes.Contains(dto.EventType))
            throw new ArgumentException($"Invalid EventType '{dto.EventType}'. Must be Running, Walking, or Cycling.");

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            StartTime = dto.StartTime,
            Location = dto.Location,
            City = dto.City,
            Province = dto.Province,
            EventType = dto.EventType,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return MapToDto(ev);
    }

    public async Task<EventResponseDto?> UpdateEventAsync(int eventId, int organiserId, UpdateEventDto dto)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (ev is null) return null;

        // Ensure the organiser owns this event
        if (ev.OrganiserId != organiserId)
            throw new UnauthorizedAccessException("You can only update your own events.");

        // Validate EventType and Status
        var validTypes = new[] { "Running", "Walking", "Cycling" };
        if (!validTypes.Contains(dto.EventType))
            throw new ArgumentException($"Invalid EventType '{dto.EventType}'.");

        var validStatuses = new[] { "Draft", "Published", "Completed", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
            throw new ArgumentException($"Invalid Status '{dto.Status}'.");

        ev.Name = dto.Name;
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.StartTime = dto.StartTime;
        ev.Location = dto.Location;
        ev.City = dto.City;
        ev.Province = dto.Province;
        ev.EventType = dto.EventType;
        ev.Status = dto.Status;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToDto(ev);
    }

    public async Task<bool> DeleteEventAsync(int eventId, int organiserId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return false;

        if (ev.OrganiserId != organiserId)
            throw new UnauthorizedAccessException("You can only delete your own events.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return true;
    }

    private static EventResponseDto MapToDto(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser is not null ? $"{e.Organiser.FirstName} {e.Organiser.LastName}" : string.Empty,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        StartTime = e.StartTime,
        Location = e.Location,
        City = e.City,
        Province = e.Province,
        EventType = e.EventType,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Categories = e.Categories.Select(c => new CategoryResponseDto
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            Distance = c.Distance,
            MaxParticipants = c.MaxParticipants,
            EntryFee = c.EntryFee,
            CreatedAt = c.CreatedAt
        }).ToList()
    };
}
