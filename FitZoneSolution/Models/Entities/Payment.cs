using System.ComponentModel.DataAnnotations.Schema;

namespace FitZone.Models.Entities;

public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    Wallet = 2
}

public class Payment
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public PaymentMethod Method { get; set; }
}
