using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // Получение списка пользователей
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = _userManager.Users.ToList();
        var result = new List<object>();

        foreach (var u in users)
        {
            result.Add(new
            {
                u.Id,
                u.Email,
                u.FullName,
                Roles = await _userManager.GetRolesAsync(u)
            });
        }

        return Ok(result);
    }

    // Изменение роли пользователя
    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(
        string id,
        ChangeUserRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound("Пользователь не найден");
        }

        string[] allowedRoles = { "Admin", "Manager", "User" };

        if (!allowedRoles.Contains(dto.Role))
        {
            return BadRequest("Указана неверная роль");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        var removeResult = await _userManager.RemoveFromRolesAsync(
            user,
            currentRoles
        );

        if (!removeResult.Succeeded)
        {
            return BadRequest(removeResult.Errors);
        }

        var addResult = await _userManager.AddToRoleAsync(
            user,
            dto.Role
        );

        if (!addResult.Succeeded)
        {
            return BadRequest(addResult.Errors);
        }

        return Ok("Роль пользователя изменена");
    }

    // Удаление пользователя
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound("Пользователь не найден");
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok("Пользователь удалён");
    }
}