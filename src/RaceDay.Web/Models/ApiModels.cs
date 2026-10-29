namespace RaceDay.Web.Models;

/// <summary>
/// Wrapper returned by <see cref="Services.IRaceDayApiClient"/> for API calls.
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; }

    public static ApiResult<T> Ok(T data, int statusCode = 200) =>
        new() { Success = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(string message, int statusCode = 400) =>
        new() { Success = false, ErrorMessage = message, StatusCode = statusCode };
}

public class AuthResponseModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class EventModel
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
    public List<CategoryModel> Categories { get; set; } = new();
}

public class CategoryModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
    public int EnrolledCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? SpotsRemaining => MaxParticipants.HasValue
        ? Math.Max(0, MaxParticipants.Value - EnrolledCount)
        : null;

    public bool IsFull => MaxParticipants.HasValue && EnrolledCount >= MaxParticipants.Value;
}

public class EnrolmentModel
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

public class ResultModel
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

public class UserProfileModel
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

