namespace VideoStudio.Models;

public class Booking
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ServiceId { get; set; }

    public int? ScheduleId { get; set; }

    public DateTime BookingDate { get; set; }

    public string? Comment { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Service? Service { get; set; }

    public Schedule? Schedule { get; set; }
}