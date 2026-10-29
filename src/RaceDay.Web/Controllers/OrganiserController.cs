using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class OrganiserController : Controller
{
    private readonly IRaceDayApiClient _api;

    public OrganiserController(IRaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var eventsResult = await _api.GetMyOrganiserEventsAsync(ct);
        if (!eventsResult.Success)
            TempData["ErrorMessage"] = eventsResult.ErrorMessage;

        var vm = new OrganiserDashboardViewModel
        {
            OrganiserName = HttpContext.Session.GetFullName(),
            Events = eventsResult.Data ?? new List<EventModel>()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Events(CancellationToken ct)
    {
        var eventsResult = await _api.GetMyOrganiserEventsAsync(ct);
        if (!eventsResult.Success)
            TempData["ErrorMessage"] = eventsResult.ErrorMessage;

        return View(eventsResult.Data ?? new List<EventModel>());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View(new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var createResult = await _api.CreateEventAsync(model, ct);
        if (!createResult.Success || createResult.Data is null)
        {
            ModelState.AddModelError(string.Empty, createResult.ErrorMessage ?? "Could not create event.");
            return View(model);
        }

        var created = createResult.Data;

        // If organiser chose a status other than Draft (e.g. Published), update it immediately
        if (!string.Equals(model.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            await _api.UpdateEventAsync(created.EventId, model, ct);
        }

        // Upload banner to Azure Blob Storage via API if provided
        if (model.BannerFile is not null && model.BannerFile.Length > 0)
        {
            var bannerResult = await _api.UploadEventBannerAsync(created.EventId, model.BannerFile, ct);
            if (!bannerResult.Success)
            {
                TempData["ErrorMessage"] = $"Event created, but banner upload failed: {bannerResult.ErrorMessage}";
                return RedirectToAction(nameof(EditEvent), new { id = created.EventId });
            }
        }

        TempData["SuccessMessage"] = $"Event '{created.Name}' created! You can now add race categories below.";
        return RedirectToAction(nameof(Categories), new { eventId = created.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id, CancellationToken ct)
    {
        var evResult = await _api.GetEventByIdAsync(id, ct);
        if (!evResult.Success || evResult.Data is null)
        {
            TempData["ErrorMessage"] = evResult.ErrorMessage ?? "Event not found.";
            return RedirectToAction(nameof(Events));
        }

        var ev = evResult.Data;
        var vm = new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            StartTime = ev.StartTime,
            Location = ev.Location,
            City = ev.City,
            Province = ev.Province,
            Distance = ev.Distance,
            EventType = ev.EventType,
            Status = ev.Status,
            ExistingBannerUrl = ev.BannerImageUrl
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model, CancellationToken ct)
    {
        model.EventId = id;
        if (!ModelState.IsValid)
            return View(model);

        var updateResult = await _api.UpdateEventAsync(id, model, ct);
        if (!updateResult.Success || updateResult.Data is null)
        {
            ModelState.AddModelError(string.Empty, updateResult.ErrorMessage ?? "Could not update event.");
            return View(model);
        }

        if (model.BannerFile is not null && model.BannerFile.Length > 0)
        {
            var bannerResult = await _api.UploadEventBannerAsync(id, model.BannerFile, ct);
            if (!bannerResult.Success)
            {
                ModelState.AddModelError(string.Empty, bannerResult.ErrorMessage ?? "Banner upload failed.");
                model.ExistingBannerUrl = updateResult.Data.BannerImageUrl;
                return View(model);
            }
        }

        TempData["SuccessMessage"] = $"Event '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        var deleteResult = await _api.DeleteEventAsync(id, ct);
        if (!deleteResult.Success)
            TempData["ErrorMessage"] = deleteResult.ErrorMessage ?? "Could not delete the event.";
        else
            TempData["SuccessMessage"] = "Event deleted.";

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int? eventId = null, CancellationToken ct = default)
    {
        var myEventsResult = await _api.GetMyOrganiserEventsAsync(ct);
        var myEvents = myEventsResult.Data ?? new List<EventModel>();

        var selectedEvent = eventId.HasValue
            ? myEvents.FirstOrDefault(e => e.EventId == eventId.Value)
            : myEvents.FirstOrDefault();

        var vm = new CategoryManagementViewModel
        {
            OrganiserEvents = myEvents,
            SelectedEvent = selectedEvent,
            Categories = selectedEvent?.Categories ?? new List<CategoryModel>(),
            NewCategory = new CreateCategoryInputModel
            {
                EventId = selectedEvent?.EventId ?? 0,
                Distance = selectedEvent?.Distance ?? 10.0m
            }
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory([Bind(Prefix = "NewCategory")] CreateCategoryInputModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please provide a valid category name, distance, and fee.";
            return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
        }

        var result = await _api.CreateCategoryAsync(model, ct);
        if (!result.Success)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not add category.";
        else
            TempData["SuccessMessage"] = $"Category '{model.Name}' added to event.";

        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryId, int eventId, CancellationToken ct)
    {
        var result = await _api.DeleteCategoryAsync(categoryId, ct);
        if (!result.Success)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not delete category.";
        else
            TempData["SuccessMessage"] = "Category removed.";

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments(
        int? eventId = null,
        string? statusFilter = null,
        int? categoryFilter = null,
        CancellationToken ct = default)
    {
        var myEventsResult = await _api.GetMyOrganiserEventsAsync(ct);
        var myEvents = myEventsResult.Data ?? new List<EventModel>();

        var selectedEvent = eventId.HasValue
            ? myEvents.FirstOrDefault(e => e.EventId == eventId.Value)
            : myEvents.FirstOrDefault();

        var enrolments = new List<EnrolmentModel>();
        if (selectedEvent is not null)
        {
            var enrResult = await _api.GetEnrolmentsByEventAsync(selectedEvent.EventId, ct);
            if (enrResult.Success && enrResult.Data is not null)
            {
                enrolments = enrResult.Data;
                if (!string.IsNullOrWhiteSpace(statusFilter))
                    enrolments = enrolments.Where(e => string.Equals(e.Status, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                if (categoryFilter.HasValue)
                    enrolments = enrolments.Where(e => e.CategoryId == categoryFilter.Value).ToList();
            }
        }

        var vm = new EnrolmentManagementViewModel
        {
            OrganiserEvents = myEvents,
            SelectedEvent = selectedEvent,
            Enrolments = enrolments,
            StatusFilter = statusFilter,
            CategoryFilter = categoryFilter
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrolmentStatus(int enrolmentId, int eventId, string status, CancellationToken ct)
    {
        var result = await _api.UpdateEnrolmentStatusAsync(enrolmentId, status, ct);
        if (!result.Success)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not update enrolment status.";
        else
            TempData["SuccessMessage"] = $"Updated enrolment status for {result.Data?.ParticipantName} to {status}.";

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int? eventId = null, CancellationToken ct = default)
    {
        var myEventsResult = await _api.GetMyOrganiserEventsAsync(ct);
        var myEvents = myEventsResult.Data ?? new List<EventModel>();

        var selectedEvent = eventId.HasValue
            ? myEvents.FirstOrDefault(e => e.EventId == eventId.Value)
            : myEvents.FirstOrDefault();

        var rows = new List<ParticipantResultRowViewModel>();
        if (selectedEvent is not null)
        {
            var enrolmentsTask = _api.GetEnrolmentsByEventAsync(selectedEvent.EventId, ct);
            var resultsTask = _api.GetResultsByEventAsync(selectedEvent.EventId, ct);
            await Task.WhenAll(enrolmentsTask, resultsTask);

            var enrolments = enrolmentsTask.Result.Data ?? new List<EnrolmentModel>();
            var existingResults = (resultsTask.Result.Data ?? new List<ResultModel>())
                .ToDictionary(r => r.EnrolmentId);

            foreach (var enr in enrolments.Where(e => e.Status != "Withdrawn"))
            {
                existingResults.TryGetValue(enr.EnrolmentId, out var res);
                rows.Add(new ParticipantResultRowViewModel
                {
                    EnrolmentId = enr.EnrolmentId,
                    ParticipantName = enr.ParticipantName,
                    BibNumber = enr.BibNumber,
                    CategoryId = enr.CategoryId,
                    CategoryName = enr.CategoryName,
                    CategoryDistance = enr.CategoryDistance,
                    EnrolmentStatus = enr.Status,
                    ResultId = res?.ResultId,
                    FinishTimeText = res?.FinishTime?.ToString("HH:mm:ss"),
                    Position = res?.Position,
                    ResultStatus = res?.Status ?? "Finished"
                });
            }
        }

        var vm = new ResultsEntryViewModel
        {
            OrganiserEvents = myEvents,
            SelectedEvent = selectedEvent,
            Rows = rows
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(SubmitSingleResultInputModel input, CancellationToken ct)
    {
        TimeOnly? parsedTime = null;
        if (string.Equals(input.Status, "Finished", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(input.FinishTime) ||
                (!TimeOnly.TryParseExact(input.FinishTime.Trim(), "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) &&
                 !TimeOnly.TryParse(input.FinishTime.Trim(), CultureInfo.InvariantCulture, out t)))
            {
                TempData["ErrorMessage"] = "Please enter a valid finish time in HH:mm:ss format (e.g. 01:42:15) for finished runners.";
                return RedirectToAction(nameof(Results), new { eventId = input.EventId });
            }
            parsedTime = t;
        }

        var result = await _api.SubmitResultAsync(input.EnrolmentId, parsedTime, input.Position, input.Status, ct);
        if (!result.Success)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not record result.";
        else
            TempData["SuccessMessage"] = $"Result saved for {result.Data?.ParticipantName} ({result.Data?.PositionDisplay}).";

        return RedirectToAction(nameof(Results), new { eventId = input.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishEventResults(int eventId, CancellationToken ct)
    {
        var evResult = await _api.GetEventByIdAsync(eventId, ct);
        if (!evResult.Success || evResult.Data is null)
        {
            TempData["ErrorMessage"] = "Event not found.";
            return RedirectToAction(nameof(Results));
        }

        var ev = evResult.Data;
        var updateModel = new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            StartTime = ev.StartTime,
            Location = ev.Location,
            City = ev.City,
            Province = ev.Province,
            Distance = ev.Distance,
            EventType = ev.EventType,
            Status = "Completed"
        };

        var updateResult = await _api.UpdateEventAsync(eventId, updateModel, ct);
        if (!updateResult.Success)
            TempData["ErrorMessage"] = updateResult.ErrorMessage ?? "Could not publish event results.";
        else
            TempData["SuccessMessage"] = $"Event '{ev.Name}' marked as Completed and official results published!";

        return RedirectToAction(nameof(Results), new { eventId });
    }
}

