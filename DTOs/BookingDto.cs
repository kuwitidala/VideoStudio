
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class BookingDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Некорректный ID услуги")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Некорректный ID расписания")]
    public int ScheduleId { get; set; }

    [StringLength(500, ErrorMessage = "Комментарий не должен превышать 500 символов")]
    public string? Comment { get; set; }
}
