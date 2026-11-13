
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStudio.Data;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Получение своих уведомлений
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync();

        return Ok(notifications);
    }

    // Отметка уведомления как прочитанного
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return NotFound("Уведомление не найдено");
        }

        notification.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok("Уведомление отмечено как прочитанное");
    }
}
