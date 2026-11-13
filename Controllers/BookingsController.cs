using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VideoStudio.Data;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Получение своих записей
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var bookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Service)
            .Include(b => b.Schedule)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings);
    }

    // Создание записи
    [HttpPost]
    public async Task<IActionResult> CreateBooking(BookingDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var service = await _context.Services
            .FirstOrDefaultAsync(s =>
                s.Id == dto.ServiceId && s.IsActive);

        if (service == null)
        {
            return BadRequest("Услуга не найдена или недоступна");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var affectedRows = await _context.Schedules
            .Where(s =>
                s.Id == dto.ScheduleId &&
                s.IsAvailable &&
                s.StartAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(s => s.IsAvailable, false));

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return BadRequest("Временной слот недоступен или уже занят");
        }

        var scheduleStartAt = await _context.Schedules
            .Where(s => s.Id == dto.ScheduleId)
            .Select(s => s.StartAt)
            .FirstAsync();

        var booking = new Booking
        {
            UserId = userId!,
            ServiceId = dto.ServiceId,
            ScheduleId = dto.ScheduleId,
            BookingDate = scheduleStartAt,
            Comment = dto.Comment,
            Status = "Pending"
        };

        var notification = new Notification
        {
            UserId = userId!,
            Title = "Бронирование создано",
            Message = $"Вы записались на услугу «{service.Name}». " +
                    $"Дата и время (UTC): {scheduleStartAt:dd.MM.yyyy HH:mm}. " +
                    "Ожидайте подтверждения записи."
        };

        _context.Bookings.Add(booking);
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(booking);
    }


    // Получение всех заявок менеджером или администратором
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetAllBookings()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Schedule)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings);
    }

    // Изменение статуса заявки
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(
        int id,
        [FromBody] string status)
    {
        var allowedStatuses = new[]
        {
            "Pending",
            "Confirmed",
            "Rejected",
            "Cancelled"
        };

        if (!allowedStatuses.Contains(status))
        {
            return BadRequest("Недопустимый статус бронирования");
        }

        var booking = await _context.Bookings
            .Include(b => b.Schedule)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound("Запись не найдена");
        }

        if ((booking.Status == "Cancelled" ||
            booking.Status == "Rejected") &&
            (status == "Pending" || status == "Confirmed"))
        {
            return BadRequest(
                "Нельзя восстановить отменённую или отклонённую запись");
        }

        if (booking.Status == status)
        {
            return BadRequest("Запись уже имеет этот статус");
        }

        booking.Status = status;

        if (status == "Cancelled" || status == "Rejected")
        {
            if (booking.Schedule != null)
            {
                booking.Schedule.IsAvailable = true;
            }
        }

        var statusMessage = status switch
        {
            "Confirmed" => "Ваша запись подтверждена.",
            "Rejected" => "Ваша запись отклонена.",
            "Cancelled" => "Ваша запись отменена.",
            "Pending" => "Статус вашей записи изменён на «Ожидает подтверждения».",
            _ => "Статус вашей записи изменён."
        };

        var notification = new Notification
        {
            UserId = booking.UserId,
            Title = "Изменение статуса бронирования",
            Message = $"{statusMessage} Услуга: «{booking.Service?.Name ?? "Неизвестная услуга"}»."
        };

        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        return Ok(booking);
    }


    // Отмена собственной записи
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var booking = await _context.Bookings
            .Include(b => b.Schedule)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound("Запись не найдена");
        }

        if (booking.UserId != userId)
        {
            return Forbid();
        }

        if (booking.Status != "Pending" &&
            booking.Status != "Confirmed")
        {
            return BadRequest("Эту запись нельзя отменить");
        }

        booking.Status = "Cancelled";

        if (booking.Schedule != null)
        {
            booking.Schedule.IsAvailable = true;
        }

        // Получаем всех администраторов и менеджеров
        var staffUserIds = await (
            from userRole in _context.UserRoles
            join role in _context.Roles
                on userRole.RoleId equals role.Id
            where role.Name == "Admin" ||
                role.Name == "Manager"
            select userRole.UserId
        ).Distinct().ToListAsync();

        var serviceName = booking.Service?.Name
            ?? "Неизвестная услуга";

        var bookingDate = booking.Schedule?.StartAt
            ?? booking.BookingDate;

        // Создаём уведомления для сотрудников
        var notifications = staffUserIds.Select(staffId =>
            new Notification
            {
                UserId = staffId,
                Title = "Отмена бронирования",
                Message = $"Пользователь отменил запись на услугу " +
                        $"«{serviceName}». " +
                        $"Дата и время (UTC): {bookingDate:dd.MM.yyyy HH:mm}."
            }).ToList();

        _context.Notifications.AddRange(notifications);

        await _context.SaveChangesAsync();

        return Ok("Запись отменена, временной слот освобождён");
    }
}