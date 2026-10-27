using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    /// Get all categories for an event.
    [HttpGet("event/{eventId:int}")]
    [ProducesResponseType(typeof(List<CategoryResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var categories = await _categoryService.GetCategoriesByEventAsync(eventId);
        return Ok(categories);
    }

    /// Create a new category for an event. Organisers only.
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var organiserId = GetUserId();
        var result = await _categoryService.CreateCategoryAsync(organiserId, dto);
        return CreatedAtAction(nameof(GetByEvent), new { eventId = result.EventId }, result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}
