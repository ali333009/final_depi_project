using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.Entities;

public enum UserRole
{
    Member = 0,
    Trainer = 1,
    Admin = 2
}

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public Member? Member { get; set; }
    public Trainer? Trainer { get; set; }
}
