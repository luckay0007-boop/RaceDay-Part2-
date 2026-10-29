using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs.Enrolments;
using RaceDay.Api.Middleware;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrolmentsController : ControllerBase
{
    private readonly IEnrolmentService _enrolmentService;

    public EnrolmentsController(IEnrolmentService enrolmentService) => _enrolmentService = enrolmentService;

    /// Register for a race category. Participants only.
    [HttpPost]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEnrolmentDto dto)
    {
        var participantId = GetUserId();
        var result = await _enrolmentService.CreateEnrolmentAsync(participantId, dto);
        return CreatedAtAction(nameof(GetByUser), new { userId = participantId }, result);
    }

    /// Get all enrolments for the currently authenticated Participant.
    [HttpGet("my")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserId();
        var enrolments = await _enrolmentService.GetEnrolmentsByUserAsync(userId);
        return Ok(enrolments);
    }

    /// Get all enrolments for a specific user (must be the user themselves or an Organiser).
    [HttpGet("user/{userId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(List<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var callerId = GetUserId();
        if (callerId != userId && !User.IsInRole("Organiser"))
            throw new ForbiddenAccessException("You can only view your own enrolments.");

        var enrolments = await _enrolmentService.GetEnrolmentsByUserAsync(userId);
        return Ok(enrolments);
    }

    /// Get all enrolments for a specific event. Organisers only (own events).
    [HttpGet("event/{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var organiserId = GetUserId();
        var enrolments = await _enrolmentService.GetEnrolmentsByEventAsync(eventId, organiserId);
        return Ok(enrolments);
    }

    /// Update the status of an enrolment (Confirmed, Pending, Registered, Withdrawn, DNS). Organisers only.
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusDto dto)
    {
        var organiserId = GetUserId();
        var updated = await _enrolmentService.UpdateEnrolmentStatusAsync(id, organiserId, dto.Status);
        if (updated is null) return NotFound();
        return Ok(updated);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}
