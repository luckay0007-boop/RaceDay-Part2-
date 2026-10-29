using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs.Results;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly IResultService _resultService;

    public ResultsController(IResultService resultService) => _resultService = resultService;

    /// Submit or update a race result. Organisers only (own events).
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Submit([FromBody] SubmitResultDto dto)
    {
        var organiserId = GetUserId();
        var result = await _resultService.SubmitResultAsync(organiserId, dto);
        return CreatedAtAction(nameof(GetByEvent), new { eventId = result.EventId }, result);
    }

    /// Get all results for an event.
    [HttpGet("event/{eventId:int}")]
    [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var results = await _resultService.GetResultsByEventAsync(eventId);
        return Ok(results);
    }

    /// Get the personal race history across completed events for the authenticated Participant.
    [HttpGet("my")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = GetUserId();
        var results = await _resultService.GetResultsByParticipantAsync(participantId);
        return Ok(results);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}
