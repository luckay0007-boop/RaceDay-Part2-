using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Images are validated by size, extension AND magic bytes, then stored under a
/// random GUID name (the user's original filename is never used).
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    public const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    // Allow-list: extension -> (content type, magic-byte check)
    private static readonly Dictionary<string, (string ContentType, Func<byte[], bool> IsMatch)> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ("image/jpeg", IsJpeg),
            [".jpeg"] = ("image/jpeg", IsJpeg),
            [".png"] = ("image/png", IsPng),
            [".webp"] = ("image/webp", IsWebp),
        };

    private static readonly HashSet<string> AllowedFolders = new(StringComparer.Ordinal) { "banners", "profiles" };

    private readonly BlobContainerClient _container;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _localFallbackRoot;
    private bool _containerReady;

    public BlobStorageService(
        BlobServiceClient serviceClient,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BlobStorageService> logger)
    {
        var containerName = config["AzureStorage:ContainerName"] ?? "raceday-images";
        _container = serviceClient.GetBlobContainerClient(containerName);
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _localFallbackRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "App_Data", "blob-fallback"));
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (!AllowedFolders.Contains(folder))
            throw new ArgumentException("Invalid upload folder.");

        if (file is null || file.Length == 0)
            throw new ArgumentException("Please choose an image to upload.");

        if (file.Length > MaxImageBytes)
            throw new ArgumentException("Image is too large. The maximum size is 5 MB.");

        // Only the extension is taken from the client filename - never the name itself.
        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        if (string.IsNullOrEmpty(extension) || !AllowedTypes.TryGetValue(extension, out var type))
            throw new ArgumentException("Only JPG, PNG or WEBP images are allowed.");

        // Verify file signature (magic bytes) so a renamed script/executable is rejected.
        var header = new byte[12];
        await using (var peek = file.OpenReadStream())
        {
            var read = await peek.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            if (read < 4 || !type.IsMatch(header))
                throw new ArgumentException("The uploaded file is not a valid image.");
        }

        // TODO(security): Integrate with an antivirus/CDR scanning service in production before persisting uploads.
        var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var blobName = $"{folder}/{safeFileName}";

        try
        {
            await EnsureContainerAsync(ct);
            var blob = _container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = type.ContentType,
                    CacheControl = "public, max-age=31536000"
                }
            }, ct);

            _logger.LogInformation("Uploaded image blob {BlobName} ({Bytes} bytes) to Azure Blob Storage", blobName, file.Length);
            return blob.Uri.ToString();
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException or HttpRequestException or TaskCanceledException)
        {
            // When running locally without Azurite started, persist safely outside web root so the API remains functional.
            _logger.LogWarning(ex, "Azure Blob Storage endpoint unreachable; saving {BlobName} to local API fallback storage.", blobName);

            var folderDir = Path.GetFullPath(Path.Combine(_localFallbackRoot, folder));
            if (!folderDir.StartsWith(_localFallbackRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid storage directory boundary.");

            Directory.CreateDirectory(folderDir);
            var targetPath = Path.GetFullPath(Path.Combine(folderDir, safeFileName));
            if (!targetPath.StartsWith(folderDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid target file path.");

            await using var outStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await using var inStream = file.OpenReadStream();
            await inStream.CopyToAsync(outStream, ct);

            var req = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = req is not null ? $"{req.Scheme}://{req.Host}" : string.Empty;
            return $"{baseUrl}/api/Media/{folder}/{safeFileName}";
        }
    }

    public async Task DeleteAsync(string? blobUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl)) return;

        // Check if it is a local fallback URL (/api/Media/{folder}/{fileName})
        const string mediaMarker = "/api/Media/";
        var markerIndex = blobUrl.IndexOf(mediaMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex >= 0)
        {
            var relative = blobUrl[(markerIndex + mediaMarker.Length)..];
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && AllowedFolders.Contains(parts[0]))
            {
                var cleanFile = Path.GetFileName(parts[1]);
                var folderDir = Path.GetFullPath(Path.Combine(_localFallbackRoot, parts[0]));
                var fullPath = Path.GetFullPath(Path.Combine(folderDir, cleanFile));
                if (fullPath.StartsWith(folderDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                {
                    try { File.Delete(fullPath); } catch { /* ignore cleanup errors */ }
                }
            }
            return;
        }

        // Only delete blobs that live in our own Azure Blob container.
        var containerUri = _container.Uri.ToString().TrimEnd('/') + "/";
        if (!blobUrl.StartsWith(containerUri, StringComparison.OrdinalIgnoreCase)) return;

        var blobName = Uri.UnescapeDataString(blobUrl[containerUri.Length..]);
        if (blobName.Contains("..")) return;

        try
        {
            await _container.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Failing to clean up an old image must not break the user's request.
            _logger.LogWarning(ex, "Could not delete old blob {BlobName}", blobName);
        }
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerReady) return;
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        _containerReady = true;
    }

    private static bool IsJpeg(byte[] h) => h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;

    private static bool IsPng(byte[] h) =>
        h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;

    private static bool IsWebp(byte[] h) =>
        h.Length >= 12 &&
        h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&   // RIFF
        h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;   // WEBP
}
