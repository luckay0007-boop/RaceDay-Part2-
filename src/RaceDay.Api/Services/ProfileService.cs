using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Profile;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class ProfileService : IProfileService
{
    private static readonly string[] ValidGenders = { "Male", "Female", "Other" };

    private readonly RaceDayDbContext _db;

    public ProfileService(RaceDayDbContext db) => _db = db;

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("User profile not found.");

        return MapToDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("User profile not found.");

        var gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
        if (gender is not null && !ValidGenders.Contains(gender))
            throw new ArgumentException($"Invalid gender '{gender}'. Must be Male, Female, or Other.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.DateOfBirth = dto.DateOfBirth;
        user.Gender = gender;
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
        user.Province = string.IsNullOrWhiteSpace(dto.Province) ? null : dto.Province.Trim();

        await _db.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<string?> GetProfilePictureUrlAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("User profile not found.");
        return user.ProfilePictureUrl;
    }

    public async Task<UserProfileDto> SetProfilePictureAsync(int userId, string pictureUrl)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("User profile not found.");

        user.ProfilePictureUrl = pictureUrl;
        await _db.SaveChangesAsync();

        return MapToDto(user);
    }

    private static UserProfileDto MapToDto(User user) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "Participant",
        DateOfBirth = user.DateOfBirth,
        Gender = user.Gender,
        PhoneNumber = user.PhoneNumber,
        City = user.City,
        Province = user.Province,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };
}

