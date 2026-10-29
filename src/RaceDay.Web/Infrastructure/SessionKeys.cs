namespace RaceDay.Web.Infrastructure;

public static class SessionKeys
{
    public const string Token = "RaceDay.JwtToken";
    public const string UserId = "RaceDay.UserId";
    public const string FullName = "RaceDay.FullName";
    public const string Email = "RaceDay.Email";
    public const string Role = "RaceDay.Role";
    public const string ProfilePictureUrl = "RaceDay.ProfilePictureUrl";
}

public static class SessionAuthExtensions
{
    public static bool IsAuthenticated(this ISession session) =>
        !string.IsNullOrWhiteSpace(session.GetString(SessionKeys.Token));

    public static string? GetJwtToken(this ISession session) =>
        session.GetString(SessionKeys.Token);

    public static int? GetUserId(this ISession session) =>
        session.GetInt32(SessionKeys.UserId);

    public static string GetFullName(this ISession session) =>
        session.GetString(SessionKeys.FullName) ?? "Athlete";

    public static string GetEmail(this ISession session) =>
        session.GetString(SessionKeys.Email) ?? string.Empty;

    public static string GetRole(this ISession session) =>
        session.GetString(SessionKeys.Role) ?? string.Empty;

    public static string? GetProfilePictureUrl(this ISession session) =>
        session.GetString(SessionKeys.ProfilePictureUrl);

    public static bool IsOrganiser(this ISession session) =>
        session.IsAuthenticated() &&
        string.Equals(session.GetRole(), "Organiser", StringComparison.OrdinalIgnoreCase);

    public static bool IsParticipant(this ISession session) =>
        session.IsAuthenticated() &&
        string.Equals(session.GetRole(), "Participant", StringComparison.OrdinalIgnoreCase);

    public static void SetAuthenticatedUser(
        this ISession session,
        int userId,
        string fullName,
        string email,
        string role,
        string token,
        string? profilePictureUrl)
    {
        session.SetInt32(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.FullName, fullName);
        session.SetString(SessionKeys.Email, email);
        session.SetString(SessionKeys.Role, role);
        session.SetString(SessionKeys.Token, token);
        if (!string.IsNullOrWhiteSpace(profilePictureUrl))
            session.SetString(SessionKeys.ProfilePictureUrl, profilePictureUrl);
        else
            session.Remove(SessionKeys.ProfilePictureUrl);
    }
}

