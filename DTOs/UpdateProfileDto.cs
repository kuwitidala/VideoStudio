
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Укажите имя")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Имя должно содержать от 2 до 100 символов")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некорректный номер телефона")]
    [StringLength(20, ErrorMessage = "Номер телефона слишком длинный")]
    public string? PhoneNumber { get; set; }
}
