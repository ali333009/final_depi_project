using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitZone.Models.Entities;

public class SubscriptionPlan
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string PlanName { get; set; } = string.Empty;

    [Range(1, 36)]
    public int DurationMonths { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
