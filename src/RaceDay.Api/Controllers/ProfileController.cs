using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs.Profile;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IBlobStorageService _blobStorage;

    public ProfileController(IProfileService profileService, IBlobStorageService blobStorage)
    {
        _profileService = profileService;
        _blobStorage = blobStorage;
    }

    /// Get the current authenticated user's profile.
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileAsync(userId);
        return Ok(profile);
    }

    /// Update the current authenticated user's personal information.
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var userId = GetUserId();
        var updated = await _profileService.UpdateProfileAsync(userId, dto);
        return Ok(updated);
    }

    /// Upload or replace the current authenticated user's profile picture in Azure Blob Storage.
    [HttpPost("me/picture")]
    [RequestSizeLimit(BlobStorageService.MaxImageBytes + 64 * 1024)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPicture(IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();
        var previousPicture = await _profileService.GetProfilePictureUrlAsync(userId);
        var pictureUrl = await _blobStorage.UploadImageAsync(file, "profiles", ct);
        var updated = await _profileService.SetProfilePictureAsync(userId, pictureUrl);

        if (!string.IsNullOrWhiteSpace(previousPicture))
            await _blobStorage.DeleteAsync(previousPicture, ct);

        return Ok(updated);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found in token.");
        return int.Parse(claim.Value);
    }
}

