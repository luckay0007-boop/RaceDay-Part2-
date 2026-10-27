using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Enrolments;

public class CreateEnrolmentDto
{
    [Required]
    public int CategoryId { get; set; }
}

public class EnrolmentResponseDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string? BibNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}
