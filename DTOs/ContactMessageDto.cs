
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class ContactMessageDto
{
    [Required(ErrorMessage = "Укажите имя")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Имя должно содержать от 2 до 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите email")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [StringLength(254, ErrorMessage = "Email слишком длинный")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите сообщение")]
    [StringLength(3000, MinimumLength = 5,
        ErrorMessage = "Сообщение должно содержать от 5 до 3000 символов")]
    public string Message { get; set; } = string.Empty;
}
