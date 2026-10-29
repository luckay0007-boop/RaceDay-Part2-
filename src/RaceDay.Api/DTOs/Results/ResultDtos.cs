using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Results;

public class SubmitResultDto
{
    [Required]
    public int EnrolmentId { get; set; }

    /// Finish time in HH:mm:ss format. Null for DNF/DSQ.
    public TimeOnly? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    /// Must be one of: Finished, DNF, DSQ.
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Finished";
}

public class ResultResponseDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public string EventCity { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal CategoryDistance { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public string PositionDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
