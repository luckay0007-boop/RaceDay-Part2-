using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class ParticipantController : Controller
{
    private readonly IRaceDayApiClient _api;

    public ParticipantController(IRaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Enrolments(CancellationToken ct)
    {
        var result = await _api.GetMyEnrolmentsAsync(ct);
        if (!result.Success)
            TempData["ErrorMessage"] = result.ErrorMessage;

        var vm = new MyEnrolmentsViewModel
        {
            Enrolments = result.Data ?? new List<EnrolmentModel>()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Results(CancellationToken ct)
    {
        var result = await _api.GetMyResultsAsync(ct);
        if (!result.Success)
            TempData["ErrorMessage"] = result.ErrorMessage;

        var vm = new MyResultsViewModel
        {
            Results = result.Data ?? new List<ResultModel>()
        };

        return View(vm);
    }
}

