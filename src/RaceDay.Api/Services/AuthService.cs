using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Auth;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public class AuthService : IAuthService
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(RaceDayDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("A user with this email address already exists.");

        // Validate role
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role)
            ?? throw new ArgumentException($"Invalid role '{dto.Role}'. Must be 'Organiser' or 'Participant'.");

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            PhoneNumber = dto.PhoneNumber,
            City = dto.City,
            Province = dto.Province,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Assign role
        _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
        await _db.SaveChangesAsync();

        // Generate JWT
        var token = GenerateJwtToken(user, role.RoleName);

        return new AuthResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = role.RoleName,
            Token = token
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roleName = user.UserRoles.FirstOrDefault()?.Role.RoleName ?? "Participant";
        var token = GenerateJwtToken(user, roleName);

        return new AuthResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = roleName,
            Token = token
        };
    }

    private string GenerateJwtToken(User user, string role)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpiryInHours"] ?? "24")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
