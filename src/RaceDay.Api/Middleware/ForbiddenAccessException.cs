namespace RaceDay.Api.Middleware;

/// <summary>
/// Thrown when an authenticated user tries to access or modify a resource
/// they do not own. Mapped to HTTP 403 Forbidden by the exception middleware
/// (as opposed to 401, which means "not logged in").
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message) { }
}
