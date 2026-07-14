namespace FitZone.Models.Entities;

public class WorkoutPlan
{
    public int Id { get; set; }

    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string PlanDetails { get; set; } = string.Empty;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
}
