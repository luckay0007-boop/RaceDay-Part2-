using RaceDay.Api.DTOs.Events;

namespace RaceDay.Api.Services;

public interface IEventService
{
    Task<List<EventResponseDto>> GetAllEventsAsync();
    Task<EventResponseDto?> GetEventByIdAsync(int eventId);
    Task<EventResponseDto> CreateEventAsync(int organiserId, CreateEventDto dto);
    Task<EventResponseDto?> UpdateEventAsync(int eventId, int organiserId, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(int eventId, int organiserId);
}
