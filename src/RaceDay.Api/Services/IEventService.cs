using RaceDay.Api.DTOs.Events;

namespace RaceDay.Api.Services;

public interface IEventService
{
    Task<List<EventResponseDto>> GetAllEventsAsync(EventFilterDto? filter = null);
    Task<List<EventResponseDto>> GetEventsByOrganiserAsync(int organiserId);
    Task<EventResponseDto?> GetEventByIdAsync(int eventId);
    Task<EventResponseDto> CreateEventAsync(int organiserId, CreateEventDto dto);
    Task<EventResponseDto?> UpdateEventAsync(int eventId, int organiserId, UpdateEventDto dto);
    Task<EventResponseDto> SetBannerAsync(int eventId, int organiserId, string bannerUrl);
    Task<string?> GetBannerUrlForOwnerAsync(int eventId, int organiserId);
    Task<bool> DeleteEventAsync(int eventId, int organiserId);
}
