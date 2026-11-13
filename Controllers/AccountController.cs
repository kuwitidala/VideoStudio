
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    // Регистрация
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(
            user,
            model.Password
        );

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "User"
        );

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(500, new
            {
                message = "Не удалось назначить роль пользователю"
            });
        }

        return Ok(new
        {
            message = "Регистрация прошла успешно"
        });
    }

    // Вход
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Неверный email или пароль"
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            model.Password,
            false
        );

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Неверный email или пароль"
            });
        }

        var token = await GenerateJwtToken(user);

        return Ok(new
        {
            token,
            email = user.Email,
            fullName = user.FullName
        });
    }

    // Выход
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new
        {
            message = "Выход выполнен. Удалите токен на клиенте."
        });
    }

    // Создание JWT-токена
    private async Task<string> GenerateJwtToken(
        ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!
            )
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(
                    _configuration["Jwt:ExpiresInMinutes"]!
                )
            ),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Модель регистрации
public class RegisterModel
{
    [Required(ErrorMessage = "Укажите имя")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль")]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "Пароль должен содержать от 8 до 128 символов")]
    public string Password { get; set; } = string.Empty;
}

// Модель входа
public class LoginModel
{
    [Required(ErrorMessage = "Укажите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль")]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
