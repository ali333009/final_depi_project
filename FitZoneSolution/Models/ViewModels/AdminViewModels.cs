using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.ViewModels;

public class MemberListItemVM
{
    public int MemberId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime MembershipDate { get; set; }
    public string SubscriptionStatus { get; set; } = "None";
}

public class MemberFormVM
{
    public int? MemberId { get; set; }
    public int? UserId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }
}

public class TrainerListItemVM
{
    public int TrainerId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public DateTime HireDate { get; set; }
    public int AssignedMemberCount { get; set; }
}

public class TrainerFormVM
{
    public int? TrainerId { get; set; }
    public int? UserId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Specialization { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }
}

public class AssignMemberVM
{
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public List<int> SelectedMemberIds { get; set; } = new();
    public List<MemberListItemVM> AllMembers { get; set; } = new();
}

public class SubscriptionAdminVM
{
    public int SubscriptionId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
}

public class PlanFormVM
{
    public int? Id { get; set; }

    [Required, MaxLength(100)]
    public string PlanName { get; set; } = string.Empty;

    [Range(1, 36)]
    public int DurationMonths { get; set; }

    [Range(1, 100000)]
    public decimal Price { get; set; }
}

public class ClassFormVM
{
    public int? Id { get; set; }

    [Required, MaxLength(100)]
    public string ClassName { get; set; } = string.Empty;

    [Required]
    public int TrainerId { get; set; }

    [Range(1, 200)]
    public int Capacity { get; set; }

    [Range(15, 180)]
    public int Duration { get; set; }

    public List<TrainerOptionVM> AvailableTrainers { get; set; } = new();
}

public class TrainerOptionVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}


public class WeeklyScheduleVM
{
    public List<ScheduleSlotVM> Slots { get; set; } = new();
    public List<ClassOptionForScheduleVM> AllClasses { get; set; } = new();
    public AddScheduleSlotVM NewSlot { get; set; } = new();
}

public class ScheduleSlotVM
{
    public int ScheduleId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BookedCount { get; set; }
    public int Capacity { get; set; }
}

public class ClassOptionForScheduleVM
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
}

public class AddScheduleSlotVM
{
    [Required]
    public int ClassId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required, DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required, DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }
}

// ---------- Reports ----------

public class ReportsVM
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveSubscriptionCount { get; set; }
    public int NewMembersCount { get; set; }
    public List<PopularClassVM> MostPopularClasses { get; set; } = new();
    public List<MonthlyRevenueVM> RevenueByMonth { get; set; } = new();
}

public class PopularClassVM
{
    public string ClassName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
}

public class MonthlyRevenueVM
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class AdminDashboardVM
{
    public int TotalMembers { get; set; }
    public int TotalTrainers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ClassesCount { get; set; }
    public int BookingsThisMonth { get; set; }
}
