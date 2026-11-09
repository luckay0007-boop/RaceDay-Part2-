using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public EventsController(IRaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        string? type = null,
        string? city = null,
        DateOnly? from = null,
        DateOnly? to = null,
        bool upcomingOnly = true,
        CancellationToken ct = default)
    {
        var eventsResult = await _api.GetEventsAsync(
            search: search,
            type: type,
            city: city,
            from: from,
            to: to,
            upcomingOnly: upcomingOnly,
            publishedOnly: true,
            ct: ct);

        if (!eventsResult.Success)
            TempData["ErrorMessage"] = eventsResult.ErrorMessage;

        var vm = new EventCatalogViewModel
        {
            Events = eventsResult.Data ?? new List<EventModel>(),
            Search = search,
            Type = type,
            City = city,
            From = from,
            To = to,
            UpcomingOnly = upcomingOnly
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var eventResult = await _api.GetEventByIdAsync(id, ct);
        if (!eventResult.Success || eventResult.Data is null)
        {
            TempData["ErrorMessage"] = eventResult.ErrorMessage ?? "The requested event could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var ev = eventResult.Data;
        var resultsResponse = await _api.GetResultsByEventAsync(id, ct);
        var userEnrolments = new List<EnrolmentModel>();

        if (HttpContext.Session.IsParticipant())
        {
            var myEnrolments = await _api.GetMyEnrolmentsAsync(ct);
            if (myEnrolments.Success && myEnrolments.Data is not null)
            {
                userEnrolments = myEnrolments.Data.Where(e => e.EventId == id).ToList();
            }
        }

        var vm = new EventDetailsViewModel
        {
            Event = ev,
            EventResults = resultsResponse.Data ?? new List<ResultModel>(),
            UserEnrolmentsForEvent = userEnrolments,
            IsParticipant = HttpContext.Session.IsParticipant(),
            IsOrganiserOwner = HttpContext.Session.IsOrganiser() && HttpContext.Session.GetUserId() == ev.OrganiserId
        };

        return View(vm);
    }

    [HttpPost]
    [RequireRole("Participant")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId, CancellationToken ct)
    {
        if (categoryId <= 0)
        {
            TempData["ErrorMessage"] = "Please select a valid race category.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.CreateEnrolmentAsync(categoryId, ct);
        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not complete enrolment.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var enrolment = result.Data;
        TempData["SuccessMessage"] =
            $"Enrolment confirmed for {enrolment.EventName} ({enrolment.CategoryName})! Your race bib number is {enrolment.BibNumber}.";

        return RedirectToAction("Enrolments", "Participant");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            UserMessage = "An unexpected error occurred while processing your request. Please try again."
        });
    }
}

