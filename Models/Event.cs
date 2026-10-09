using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Events")]
public class Event
{
    [Key]
    public int EventId { get; set; }

    [Required]
    public int OrganiserId { get; set; }

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
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganiserId))]
    public User Organiser { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
