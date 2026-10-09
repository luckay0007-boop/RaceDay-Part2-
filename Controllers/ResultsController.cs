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

    /// Submit a race result. Organisers only.
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Submit([FromBody] SubmitResultDto dto)
    {
        var result = await _resultService.SubmitResultAsync(dto);
        return CreatedAtAction(nameof(GetByEvent), new { eventId = 0 }, result);
    }

    /// Get all results for an event.
    [HttpGet("event/{eventId:int}")]
    [ProducesResponseType(typeof(List<ResultResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var results = await _resultService.GetResultsByEventAsync(eventId);
        return Ok(results);
    }
}
