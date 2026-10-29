using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is required."), MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required."), EmailAddress, MaxLength(255)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(128)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an account role.")]
    [Display(Name = "Account Role")]
    public string Role { get; set; } = "Participant";

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(10)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Display(Name = "Province")]
    public string? Province { get; set; }
}

