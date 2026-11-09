using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events;

public class CreateCategoryDto
{
    [Required]
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0.01, 999.99)]
    public decimal Distance { get; set; }

    [Range(1, 100000)]
    public int? MaxParticipants { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; } = 0.00m;
}

public class CategoryResponseDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
    /// Number of active (non-withdrawn) enrolments in this category.
    public int EnrolledCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
