using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.Entities;

public class Member
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateTime MembershipDate { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
    public ICollection<ProgressTracking> ProgressRecords { get; set; } = new List<ProgressTracking>();
}
