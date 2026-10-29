using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class MediaController : ControllerBase
{
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.Ordinal) { "banners", "profiles" };
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    [GeneratedRegex(@"^[a-f0-9]{32}\.(jpg|jpeg|png|webp)$", RegexOptions.IgnoreCase)]
    private static partial Regex SafeFileNameRegex();

    [HttpGet("{folder}/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetImage(string folder, string fileName)
    {
        if (!AllowedFolders.Contains(folder))
            return NotFound();

        var cleanFile = Path.GetFileName(fileName);
        if (!SafeFileNameRegex().IsMatch(cleanFile))
            return NotFound();

        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "App_Data", "blob-fallback", folder));
        var fullPath = Path.GetFullPath(Path.Combine(rootDir, cleanFile));
        if (!fullPath.StartsWith(rootDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var ext = Path.GetExtension(cleanFile);
        if (!ContentTypes.TryGetValue(ext, out var contentType))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "public, max-age=86400";
        return PhysicalFile(fullPath, contentType);
    }
}

