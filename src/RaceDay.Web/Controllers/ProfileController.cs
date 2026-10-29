using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin]
public class ProfileController : Controller
{
    private readonly IRaceDayApiClient _api;

    public ProfileController(IRaceDayApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _api.GetMyProfileAsync(ct);
        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load your profile.";
            return RedirectToAction("Index", "Events");
        }

        return View(MapToViewModel(result.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        var result = await _api.UpdateMyProfileAsync(model, ct);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not update profile.");
            return View("Index", model);
        }

        var updated = result.Data;
        HttpContext.Session.SetString(SessionKeys.FullName, updated.FullName);
        TempData["SuccessMessage"] = "Your personal profile information has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? pictureFile, CancellationToken ct)
    {
        if (pictureFile is null || pictureFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select an image file (JPG, PNG, or WEBP up to 5 MB) to upload.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(pictureFile, ct);
        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Profile picture upload failed.";
            return RedirectToAction(nameof(Index));
        }

        var updated = result.Data;
        if (!string.IsNullOrWhiteSpace(updated.ProfilePictureUrl))
            HttpContext.Session.SetString(SessionKeys.ProfilePictureUrl, updated.ProfilePictureUrl);

        TempData["SuccessMessage"] = "Profile picture uploaded to Azure Blob Storage!";
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel MapToViewModel(UserProfileModel p) => new()
    {
        UserId = p.UserId,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Email = p.Email,
        Role = p.Role,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        PhoneNumber = p.PhoneNumber,
        City = p.City,
        Province = p.Province,
        ProfilePictureUrl = p.ProfilePictureUrl,
        CreatedAt = p.CreatedAt
    };
}

