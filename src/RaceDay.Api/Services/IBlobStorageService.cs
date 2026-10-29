namespace RaceDay.Api.Services;

/// <summary>
/// Abstraction over cloud file storage (implemented with Azure Blob Storage).
/// All image handling for the platform goes through the API via this service –
/// the MVC front end never talks to Azure directly.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Validates and uploads an image, returning its public URL.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="folder">Logical folder/prefix, e.g. "banners" or "profiles".</param>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>
    /// Deletes a previously uploaded blob by its URL. Silently ignores URLs that
    /// do not belong to the configured container.
    /// </summary>
    Task DeleteAsync(string? blobUrl, CancellationToken ct = default);
}
