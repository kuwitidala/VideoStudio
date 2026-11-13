
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class ReviewDto
{
    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Текст отзыва обязателен")]
    [StringLength(2000, MinimumLength = 1,
        ErrorMessage = "Отзыв должен содержать не более 2000 символов")]
    public string Text { get; set; } = string.Empty;
}
