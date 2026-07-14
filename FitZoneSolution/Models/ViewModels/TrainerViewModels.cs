using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.ViewModels;

public class TrainerDashboardVM
{
    public string TrainerName { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public int AssignedMemberCount { get; set; }
    public int ClassCount { get; set; }
    public List<UpcomingClassVM> UpcomingClasses { get; set; } = new();
}

public class AssignedMemberVM
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string SubscriptionStatus { get; set; } = "None";
}

public class CreateWorkoutVM
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string PlanDetails { get; set; } = string.Empty;
}

public class AttendanceVM
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public List<AttendanceRowVM> Rows { get; set; } = new();
}

public class AttendanceRowVM
{
    public int BookingId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class MemberProgressVM
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public List<ProgressPointVM> Progress { get; set; } = new();
}

public class TrainerScheduleVM
{
    public List<TrainerScheduleSlotVM> Slots { get; set; } = new();
}

public class TrainerScheduleSlotVM
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
