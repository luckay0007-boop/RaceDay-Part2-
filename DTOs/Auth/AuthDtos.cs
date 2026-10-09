using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Auth;

public class RegisterUserDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// Role must be "Organiser" or "Participant".
    [Required]
    public string Role { get; set; } = "Participant";

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
