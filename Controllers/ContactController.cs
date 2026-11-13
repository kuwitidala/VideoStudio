using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStudio.Data;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Отправка сообщения
    [HttpPost]
    public async Task<IActionResult> SendMessage(ContactMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest("Заполните все поля");
        }

        var contactMessage = new ContactMessage
        {
            Name = dto.Name,
            Email = dto.Email,
            Message = dto.Message
        };

        _context.ContactMessages.Add(contactMessage);
        await _context.SaveChangesAsync();

        return Ok("Сообщение успешно отправлено");
    }

    // Просмотр всех сообщений
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await _context.ContactMessages
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return Ok(messages);
    }

    // Отметить сообщение как прочитанное
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);

        if (message == null)
        {
            return NotFound("Сообщение не найдено");
        }

        message.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok("Сообщение отмечено как прочитанное");
    }

    // Удаление сообщения
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);

        if (message == null)
        {
            return NotFound("Сообщение не найдено");
        }

        _context.ContactMessages.Remove(message);
        await _context.SaveChangesAsync();

        return Ok("Сообщение удалено");
    }
}