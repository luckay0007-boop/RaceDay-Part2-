using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventCatalogViewModel
{
    public List<EventModel> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? City { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool UpcomingOnly { get; set; } = true;
}

public class EventDetailsViewModel
{
    public EventModel Event { get; set; } = new();
    public List<EnrolmentModel> UserEnrolmentsForEvent { get; set; } = new();
    public List<ResultModel> EventResults { get; set; } = new();
    public bool IsParticipant { get; set; }
    public bool IsOrganiserOwner { get; set; }

    public bool IsEnrolledInCategory(int categoryId) =>
        UserEnrolmentsForEvent.Any(e => e.CategoryId == categoryId && e.Status != "Withdrawn");
}

public class MyEnrolmentsViewModel
{
    public List<EnrolmentModel> Enrolments { get; set; } = new();
    public int ConfirmedCount => Enrolments.Count(e => e.Status == "Confirmed");
    public int PendingCount => Enrolments.Count(e => e.Status is "Pending" or "Registered");
    public int UpcomingCount => Enrolments.Count(e => e.EventDate >= DateOnly.FromDateTime(DateTime.UtcNow) && e.Status != "Withdrawn");
}

public class MyResultsViewModel
{
    public List<ResultModel> Results { get; set; } = new();
    public int CompletedRacesCount => Results.Count(r => r.Status == "Finished");
    public int PodiumCount => Results.Count(r => r.Status == "Finished" && r.Position is >= 1 and <= 3);
    public decimal TotalKilometresCompleted => Results.Where(r => r.Status == "Finished").Sum(r => r.CategoryDistance);
}

public class ProfileViewModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required(ErrorMessage = "First name is required."), MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(10)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Display(Name = "Province")]
    public string? Province { get; set; }
}

