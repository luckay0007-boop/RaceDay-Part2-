using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Enrolments")]
public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string? BibNumber { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Registered";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ParticipantId))]
    public User Participant { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}
