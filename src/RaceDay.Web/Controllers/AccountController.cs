using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApiClient _api;

    public AccountController(IRaceDayApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsAuthenticated())
            return RedirectToRoleHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.LoginAsync(model, ct);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid email or password.");
            return View(model);
        }

        var auth = result.Data;
        HttpContext.Session.SetAuthenticatedUser(
            auth.UserId,
            auth.FullName,
            auth.Email,
            auth.Role,
            auth.Token,
            auth.ProfilePictureUrl);

        TempData["SuccessMessage"] = $"Welcome back, {auth.FullName}!";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToRoleHome();
    }

    [HttpGet]
    public IActionResult Register(string? role = null)
    {
        if (HttpContext.Session.IsAuthenticated())
            return RedirectToRoleHome();

        var initialRole = string.Equals(role, "Organiser", StringComparison.OrdinalIgnoreCase)
            ? "Organiser"
            : "Participant";

        return View(new RegisterViewModel { Role = initialRole });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.RegisterAsync(model, ct);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration could not be completed.");
            return View(model);
        }

        var auth = result.Data;
        HttpContext.Session.SetAuthenticatedUser(
            auth.UserId,
            auth.FullName,
            auth.Email,
            auth.Role,
            auth.Token,
            auth.ProfilePictureUrl);

        TempData["SuccessMessage"] = $"Account created! Welcome to RaceDay, {auth.FullName}.";
        return RedirectToRoleHome();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        TempData["SuccessMessage"] = "You have been signed out safely.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToRoleHome()
    {
        if (HttpContext.Session.IsOrganiser())
            return RedirectToAction("Dashboard", "Organiser");

        return RedirectToAction("Index", "Events");
    }
}

