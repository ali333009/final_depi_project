using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.Entities;

public class Trainer
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(100)]
    public string? Specialization { get; set; }

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public ICollection<GymClass> Classes { get; set; } = new List<GymClass>();
    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();

   
    public ICollection<TrainerMemberAssignment> AssignedMembers { get; set; } = new List<TrainerMemberAssignment>();
}
