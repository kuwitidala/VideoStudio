using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileManagementController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileManagementController(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // Изменение данных профиля
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return NotFound("Пользователь не найден");
        }

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok("Профиль успешно обновлён");
    }

    // Смена пароля
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return NotFound("Пользователь не найден");
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok("Пароль успешно изменён");
    }
}