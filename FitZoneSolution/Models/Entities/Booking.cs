namespace FitZone.Models.Entities;

public enum BookingStatus
{
    Confirmed = 0,
    Cancelled = 1,
    Attended = 2,
    NoShow = 3
}

public class Booking
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int ClassId { get; set; }
    public GymClass Class { get; set; } = null!;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
}
