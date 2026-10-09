using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("UserRoles")]
public class UserRole
{
    [Key]
    public int UserRoleId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int RoleId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;
}
