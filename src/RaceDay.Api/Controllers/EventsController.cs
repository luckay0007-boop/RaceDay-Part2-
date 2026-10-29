using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBlobStorageService _blobStorage;

    public EventsController(IEventService eventService, IBlobStorageService blobStorage)
    {
        _eventService = eventService;
        _blobStorage = blobStorage;
    }

    /// Get all events with optional query filtering.
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] EventFilterDto filter)
    {
        var events = await _eventService.GetAllEventsAsync(filter);
        return Ok(events);
    }

    /// Get all events created by the currently authenticated Organiser.
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EventResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var organiserId = GetUserId();
        var events = await _eventService.GetEventsByOrganiserAsync(organiserId);
        return Ok(events);
    }

    /// Get a specific event by ID.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev is null) return NotFound();
        return Ok(ev);
    }

    /// Create a new event. Organisers only.
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        var organiserId = GetUserId();
        var result = await _eventService.CreateEventAsync(organiserId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.EventId }, result);
    }

    /// Update an existing event. Organisers only (own events).
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventDto dto)
    {
        var organiserId = GetUserId();
        var result = await _eventService.UpdateEventAsync(id, organiserId, dto);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// Upload or replace an event banner image in Azure Blob Storage. Organisers only (own events).
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(BlobStorageService.MaxImageBytes + 64 * 1024)]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var organiserId = GetUserId();
        var previousBanner = await _eventService.GetBannerUrlForOwnerAsync(id, organiserId);
        var bannerUrl = await _blobStorage.UploadImageAsync(file, "banners", ct);
        var updated = await _eventService.SetBannerAsync(id, organiserId, bannerUrl);

        if (!string.IsNullOrWhiteSpace(previousBanner))
            await _blobStorage.DeleteAsync(previousBanner, ct);

        return Ok(updated);
    }

    /// Delete an event. Organisers only (own events).
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var organiserId = GetUserId();
        var existing = await _eventService.GetEventByIdAsync(id);
        var deleted = await _eventService.DeleteEventAsync(id, organiserId);
        if (!deleted) return NotFound();

        if (!string.IsNullOrWhiteSpace(existing?.BannerImageUrl))
            await _blobStorage.DeleteAsync(existing.BannerImageUrl, ct);

        return NoContent();
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}
