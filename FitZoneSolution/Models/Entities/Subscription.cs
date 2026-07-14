namespace FitZone.Models.Entities;

public enum SubscriptionStatus
{
    PendingPayment = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3
}

public class Subscription
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PendingPayment;

    public bool IsDeleted { get; set; } = false;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
