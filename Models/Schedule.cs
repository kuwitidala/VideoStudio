namespace VideoStudio.Models;

public class Schedule
{
    public int Id { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string? Comment { get; set; }
}