using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Filters;

/// <summary>
/// Ensures the current user has an active authenticated session.
/// Unauthenticated users are redirected to the Login page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.IsAuthenticated())
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        // Prevent browser back-button caching of authenticated pages after logout.
        context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Ensures the current user is authenticated AND has the required role (e.g. "Organiser" or "Participant").
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : RequireLoginAttribute
{
    private readonly string _role;

    public RequireRoleAttribute(string role) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (context.Result is not null) return;

        var currentRole = context.HttpContext.Session.GetRole();
        if (!string.Equals(currentRole, _role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

