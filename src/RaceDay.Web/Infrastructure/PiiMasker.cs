namespace RaceDay.Web.Infrastructure;

public static class PiiMasker
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "—";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***" + (atIndex >= 0 ? email[atIndex..] : string.Empty);

        var local = email[..atIndex];
        var domain = email[atIndex..];
        var visiblePrefix = local[..Math.Min(2, local.Length)];
        return $"{visiblePrefix}***{domain}";
    }

    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "—";
        var trimmed = phone.Trim();
        if (trimmed.Length <= 4) return "****";
        return $"***-***-{trimmed[^4..]}";
    }
}

