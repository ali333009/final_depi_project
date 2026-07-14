namespace FitZone.Models.Entities;

public class ClassSchedule
{
    public int Id { get; set; }

    public int ClassId { get; set; }
    public GymClass Class { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
