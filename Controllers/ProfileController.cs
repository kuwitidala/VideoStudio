using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    // Информация о текущем пользователе
    [HttpGet]
    public IActionResult GetProfile()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            name = User.FindFirstValue(ClaimTypes.Name),
            roles = User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
        });
    }

    // Проверка доступа администратора
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminAccess()
    {
        return Ok(new
        {
            message = "Доступ разрешён. Вы являетесь администратором."
        });
    }

    // Проверка доступа менеджера или администратора
    [HttpGet("manager")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult ManagerAccess()
    {
        return Ok(new
        {
            message = "Доступ разрешён."
        });
    }
}