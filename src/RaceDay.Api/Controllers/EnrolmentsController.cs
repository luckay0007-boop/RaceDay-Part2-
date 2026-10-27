using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs.Enrolments;
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

    /// Get all enrolments for a specific user.
    [HttpGet("user/{userId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(List<EnrolmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var enrolments = await _enrolmentService.GetEnrolmentsByUserAsync(userId);
        return Ok(enrolments);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}
