using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class EventService : IEventService
{
    private static readonly string[] ValidTypes = { "Running", "Walking", "Cycling" };
    private static readonly string[] ValidStatuses = { "Draft", "Published", "Completed", "Cancelled" };

    private readonly RaceDayDbContext _db;

    public EventService(RaceDayDbContext db) => _db = db;

    public async Task<List<EventResponseDto>> GetAllEventsAsync(EventFilterDto? filter = null)
    {
        filter ??= new EventFilterDto();

        IQueryable<Event> query = _db.Events
            .AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments);

        if (filter.PublishedOnly)
            query = query.Where(e => e.Status == "Published" || e.Status == "Completed");

        if (filter.UpcomingOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(e => e.EventDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(e => e.EventType == filter.Type);

        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(e => e.City.Contains(filter.City));

        if (filter.From.HasValue)
            query = query.Where(e => e.EventDate >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(e => e.EventDate <= filter.To.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(e => e.Name.Contains(filter.Search)
                                  || e.Location.Contains(filter.Search)
                                  || e.City.Contains(filter.Search));

        // Upcoming lists read best soonest-first; full history reads best newest-first.
        query = filter.UpcomingOnly
            ? query.OrderBy(e => e.EventDate).ThenBy(e => e.StartTime)
            : query.OrderByDescending(e => e.EventDate);

        var events = await query.ToListAsync();
        return events.Select(MapToDto).ToList();
    }

    public async Task<List<EventResponseDto>> GetEventsByOrganiserAsync(int organiserId)
    {
        var events = await _db.Events
            .AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return events.Select(MapToDto).ToList();
    }

    public async Task<EventResponseDto?> GetEventByIdAsync(int eventId)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        return ev is null ? null : MapToDto(ev);
    }

    public async Task<EventResponseDto> CreateEventAsync(int organiserId, CreateEventDto dto)
    {
        if (!ValidTypes.Contains(dto.EventType))
            throw new ArgumentException($"Invalid event type '{dto.EventType}'. Must be Running, Walking, or Cycling.");

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            EventDate = dto.EventDate,
            StartTime = dto.StartTime,
            Location = dto.Location.Trim(),
            City = dto.City.Trim(),
            Province = dto.Province.Trim(),
            Distance = dto.Distance,
            EventType = dto.EventType,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return MapToDto(ev);
    }

    public async Task<EventResponseDto?> UpdateEventAsync(int eventId, int organiserId, UpdateEventDto dto)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (ev is null) return null;

        if (ev.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only update your own events.");

        if (!ValidTypes.Contains(dto.EventType))
            throw new ArgumentException($"Invalid event type '{dto.EventType}'.");

        if (!ValidStatuses.Contains(dto.Status))
            throw new ArgumentException($"Invalid status '{dto.Status}'.");

        ev.Name = dto.Name.Trim();
        ev.Description = dto.Description?.Trim();
        ev.EventDate = dto.EventDate;
        ev.StartTime = dto.StartTime;
        ev.Location = dto.Location.Trim();
        ev.City = dto.City.Trim();
        ev.Province = dto.Province.Trim();
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;
        ev.Status = dto.Status;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToDto(ev);
    }

    public async Task<EventResponseDto> SetBannerAsync(int eventId, int organiserId, string bannerUrl)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.");

        if (ev.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only change the banner of your own events.");

        ev.BannerImageUrl = bannerUrl;
        ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToDto(ev);
    }

    public async Task<string?> GetBannerUrlForOwnerAsync(int eventId, int organiserId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId)
            ?? throw new KeyNotFoundException($"Event with ID {eventId} not found.");

        if (ev.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only change the banner of your own events.");

        return ev.BannerImageUrl;
    }

    public async Task<bool> DeleteEventAsync(int eventId, int organiserId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return false;

        if (ev.OrganiserId != organiserId)
            throw new ForbiddenAccessException("You can only delete your own events.");

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
        Distance = e.Distance,
        EventType = e.EventType,
        Status = e.Status,
        BannerImageUrl = e.BannerImageUrl,
        EnrolmentCount = e.Categories.Sum(c => c.Enrolments.Count(en => en.Status != "Withdrawn")),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Categories = e.Categories
            .OrderBy(c => c.Distance)
            .Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                Distance = c.Distance,
                MaxParticipants = c.MaxParticipants,
                EntryFee = c.EntryFee,
                EnrolledCount = c.Enrolments.Count(en => en.Status != "Withdrawn"),
                CreatedAt = c.CreatedAt
            }).ToList()
    };
}
