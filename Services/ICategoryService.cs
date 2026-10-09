using RaceDay.Api.DTOs.Events;

namespace RaceDay.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryResponseDto>> GetCategoriesByEventAsync(int eventId);
    Task<CategoryResponseDto> CreateCategoryAsync(int organiserId, CreateCategoryDto dto);
}
