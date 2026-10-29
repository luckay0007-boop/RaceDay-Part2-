using RaceDay.Api.DTOs.Profile;

namespace RaceDay.Api.Services;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<string?> GetProfilePictureUrlAsync(int userId);
    Task<UserProfileDto> SetProfilePictureAsync(int userId, string pictureUrl);
}

