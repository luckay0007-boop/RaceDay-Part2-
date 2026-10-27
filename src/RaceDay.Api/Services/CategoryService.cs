using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly RaceDayDbContext _db;

    public CategoryService(RaceDayDbContext db) => _db = db;

    public async Task<List<CategoryResponseDto>> GetCategoriesByEventAsync(int eventId)
    {
        return await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Distance)
            .Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                Distance = c.Distance,
                MaxParticipants = c.MaxParticipants,
                EntryFee = c.EntryFee,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(int organiserId, CreateCategoryDto dto)
    {
        // Verify the event exists and belongs to this organiser
        var ev = await _db.Events.FindAsync(dto.EventId)
            ?? throw new KeyNotFoundException($"Event with ID {dto.EventId} not found.");

        if (ev.OrganiserId != organiserId)
            throw new UnauthorizedAccessException("You can only add categories to your own events.");

        var category = new Category
        {
            EventId = dto.EventId,
            Name = dto.Name,
            Distance = dto.Distance,
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return new CategoryResponseDto
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Distance = category.Distance,
            MaxParticipants = category.MaxParticipants,
            EntryFee = category.EntryFee,
            CreatedAt = category.CreatedAt
        };
    }
}
