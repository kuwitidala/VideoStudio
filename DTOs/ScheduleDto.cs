
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class ScheduleDto
{
    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    [StringLength(500, ErrorMessage = "Комментарий не должен превышать 500 символов")]
    public string? Comment { get; set; }
}
