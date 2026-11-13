
using System.ComponentModel.DataAnnotations;

namespace VideoStudio.DTOs;

public class ChangeUserRoleDto
{
    [Required(ErrorMessage = "Укажите роль")]
    [RegularExpression("^(Admin|Manager|User)$",
        ErrorMessage = "Допустимые роли: Admin, Manager, User")]
    public string Role { get; set; } = string.Empty;
}
