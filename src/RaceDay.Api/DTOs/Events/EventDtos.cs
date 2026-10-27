using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events;

public class CreateEventDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateOnly EventDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required, MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    /// Must be one of: Running, Walking, Cycling.
    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;
}

public class UpdateEventDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateOnly EventDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required, MaxLength(300)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

public class EventResponseDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CategoryResponseDto> Categories { get; set; } = new();
}
