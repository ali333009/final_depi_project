using System.ComponentModel.DataAnnotations.Schema;

namespace FitZone.Models.Entities;

public class ProgressTracking
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? BodyFat { get; set; }

    public string? Notes { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
}
