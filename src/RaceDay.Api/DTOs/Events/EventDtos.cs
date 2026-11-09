using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events;

public class CreateEventDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
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

    /// Main race distance in kilometres.
    [Required, Range(0.1, 9999.99)]
    public decimal Distance { get; set; }

    /// Must be one of: Running, Walking, Cycling.
    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;
}

public class UpdateEventDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
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

    [Required, Range(0.1, 9999.99)]
    public decimal Distance { get; set; }

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

/// Optional query-string filters for GET /api/Events.
public class EventFilterDto
{
    [MaxLength(100)]
    public string? Search { get; set; }

    [MaxLength(20)]
    public string? Type { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    /// When true, only events dated today or later are returned.
    public bool UpcomingOnly { get; set; }

    /// When true (default for anonymous/participant browsing), Draft events are hidden.
    public bool PublishedOnly { get; set; }
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
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CategoryResponseDto> Categories { get; set; } = new();
}
