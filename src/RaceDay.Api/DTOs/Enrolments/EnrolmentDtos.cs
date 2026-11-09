using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Enrolments;

public class CreateEnrolmentDto
{
    [Required]
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusDto
{
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentResponseDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal CategoryDistance { get; set; }
    public decimal EntryFee { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly EventStartTime { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public string EventCity { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventStatus { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string? BibNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}
