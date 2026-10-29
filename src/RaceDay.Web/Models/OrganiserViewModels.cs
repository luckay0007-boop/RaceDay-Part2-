using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class OrganiserDashboardViewModel
{
    public string OrganiserName { get; set; } = string.Empty;
    public List<EventModel> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingEventsCount => Events.Count(e => e.EventDate >= DateOnly.FromDateTime(DateTime.UtcNow) && e.Status != "Cancelled");
    public int CompletedEventsCount => Events.Count(e => e.Status == "Completed");
    public List<EventModel> UpcomingEvents => Events
        .Where(e => e.EventDate >= DateOnly.FromDateTime(DateTime.UtcNow) && e.Status != "Cancelled")
        .OrderBy(e => e.EventDate)
        .ToList();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Event name is required."), MaxLength(200)]
    [Display(Name = "Event Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Event date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event Date")]
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));

    [Required(ErrorMessage = "Start time is required.")]
    [DataType(DataType.Time)]
    [Display(Name = "Start Time")]
    public TimeOnly StartTime { get; set; } = new TimeOnly(6, 0);

    [Required(ErrorMessage = "Venue / location is required."), MaxLength(300)]
    [Display(Name = "Venue / Location")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required."), MaxLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Province is required."), MaxLength(100)]
    [Display(Name = "Province")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Primary race distance is required.")]
    [Range(0.1, 9999.99, ErrorMessage = "Distance must be between 0.1 km and 9999.99 km.")]
    [Display(Name = "Primary Distance (km)")]
    public decimal Distance { get; set; } = 10.0m;

    [Required(ErrorMessage = "Event type is required."), MaxLength(20)]
    [Display(Name = "Event Type")]
    public string EventType { get; set; } = "Running";

    [MaxLength(20)]
    [Display(Name = "Event Status")]
    public string Status { get; set; } = "Published";

    public string? ExistingBannerUrl { get; set; }

    [Display(Name = "Event Banner Image (JPG, PNG, WEBP up to 5 MB)")]
    public IFormFile? BannerFile { get; set; }
}

public class CategoryManagementViewModel
{
    public List<EventModel> OrganiserEvents { get; set; } = new();
    public EventModel? SelectedEvent { get; set; }
    public List<CategoryModel> Categories { get; set; } = new();
    public CreateCategoryInputModel NewCategory { get; set; } = new();
}

public class CreateCategoryInputModel
{
    [Required]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Category name is required (e.g. '21.1km Half Marathon' or 'Juniors Under-18 5km')."), MaxLength(100)]
    [Display(Name = "Category Name (Age / Distance)")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category distance is required.")]
    [Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; } = 10.0m;

    [Range(1, 100000, ErrorMessage = "Max participants must be at least 1.")]
    [Display(Name = "Max Capacity (Optional)")]
    public int? MaxParticipants { get; set; } = 500;

    [Range(0, 100000, ErrorMessage = "Entry fee cannot be negative.")]
    [Display(Name = "Entry Fee (R)")]
    public decimal EntryFee { get; set; } = 150.00m;
}

public class EnrolmentManagementViewModel
{
    public List<EventModel> OrganiserEvents { get; set; } = new();
    public EventModel? SelectedEvent { get; set; }
    public List<EnrolmentModel> Enrolments { get; set; } = new();
    public string? StatusFilter { get; set; }
    public int? CategoryFilter { get; set; }
}

public class ResultsEntryViewModel
{
    public List<EventModel> OrganiserEvents { get; set; } = new();
    public EventModel? SelectedEvent { get; set; }
    public List<ParticipantResultRowViewModel> Rows { get; set; } = new();
}

public class ParticipantResultRowViewModel
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal CategoryDistance { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public int? ResultId { get; set; }
    public string? FinishTimeText { get; set; }
    public int? Position { get; set; }
    public string ResultStatus { get; set; } = "Finished";
    public bool HasRecordedResult => ResultId.HasValue;
}

public class SubmitSingleResultInputModel
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }

    public string? FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

