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

    public EventsController(IEventService eventService) => _eventService = eventService;

    /// Get all events.
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _eventService.GetAllEventsAsync();
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

    /// Delete an event. Organisers only (own events).
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var organiserId = GetUserId();
        var deleted = await _eventService.DeleteEventAsync(id, organiserId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}
