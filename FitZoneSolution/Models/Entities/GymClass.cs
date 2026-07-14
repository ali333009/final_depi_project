using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.Entities;


public class GymClass
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ClassName { get; set; } = string.Empty;

    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;

    [Range(1, 200)]
    public int Capacity { get; set; }

    // Duration in minutes
    public int Duration { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
