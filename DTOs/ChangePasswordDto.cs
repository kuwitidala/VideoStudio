
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Укажите текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите новый пароль")]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "Пароль должен содержать от 8 до 128 символов")]
    public string NewPassword { get; set; } = string.Empty;
}
