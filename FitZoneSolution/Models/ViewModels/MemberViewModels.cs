using System.ComponentModel.DataAnnotations;

namespace FitZone.Models.ViewModels;

public class MemberDashboardVM
{
    public string MemberName { get; set; } = string.Empty;
    public string? ActivePlanName { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public string SubscriptionStatus { get; set; } = "None";
    public int UpcomingBookingCount { get; set; }
    public List<UpcomingClassVM> UpcomingClasses { get; set; } = new();
    public List<ProgressPointVM> RecentProgress { get; set; } = new();
    public string? TrainerName { get; set; }
    public string? TrainerSpecialization { get; set; }
}

public class UpcomingClassVM
{
    public int BookingId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ProgressPointVM
{
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
    public decimal? BodyFat { get; set; }
    public string? Notes { get; set; }
}

public class SubscribeVM
{
    public List<PlanOptionVM> AvailablePlans { get; set; } = new();
}

public class PlanOptionVM
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public decimal Price { get; set; }
}

public class BookClassVM
{
    public List<ClassOptionVM> AvailableClasses { get; set; } = new();
    public bool HasActiveSubscription { get; set; }
}

public class ClassOptionVM
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int Duration { get; set; }
    public List<(DayOfWeek Day, TimeSpan Start, TimeSpan End)> Schedule { get; set; } = new();
    public bool AlreadyBooked { get; set; }
    public int? ActiveBookingId { get; set; }
    public bool IsFull => BookedCount >= Capacity;
}

public class AddProgressVM
{
    [Required, Range(20, 400)]
    public decimal Weight { get; set; }

    [Range(1, 80)]
    public decimal? BodyFat { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class MyProgressVM
{
    public List<ProgressPointVM> History { get; set; } = new();
    public AddProgressVM NewEntry { get; set; } = new();
}

public class WorkoutPlanVM
{
    public int Id { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public string PlanDetails { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
}

public class MyWorkoutPlansVM
{
    public List<WorkoutPlanVM> Plans { get; set; } = new();
}

public class MySubscriptionVM
{
    public string PlanName { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysRemaining { get; set; }
    public List<PaymentHistoryVM> Payments { get; set; } = new();
}

public class PaymentHistoryVM
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class SubscriptionHistoryVM
{
    public List<PastSubscriptionVM> Subscriptions { get; set; } = new();
}

public class PastSubscriptionVM
{
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
}

public class EditProfileVM
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}

public class BookingHistoryVM
{
    public List<BookingHistoryItemVM> Bookings { get; set; } = new();
}

public class BookingHistoryItemVM
{
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Schedule { get; set; } = new();
}
