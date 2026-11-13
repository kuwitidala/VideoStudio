using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStudio.Data;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ScheduleController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Получение доступных временных слотов
    [HttpGet]
    public async Task<IActionResult> GetAvailableDates()
    {
        var dates = await _context.Schedules
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.StartAt)
            .ToListAsync();

        return Ok(dates);
    }

    // Добавление временного слота
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> CreateSchedule(ScheduleDto dto)
    {
        if (dto.StartAt >= dto.EndAt ||
            dto.StartAt <= DateTime.UtcNow)
        {
            return BadRequest("Указан неправильный временной интервал");
        }

        // Проверка пересечения с существующими слотами
        var hasConflict = await _context.Schedules
            .AnyAsync(s =>
                dto.StartAt < s.EndAt &&
                dto.EndAt > s.StartAt
            );

        if (hasConflict)
        {
            return BadRequest("Это время пересекается с существующим слотом");
        }

        var schedule = new Schedule
        {
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            Comment = dto.Comment,
            IsAvailable = true
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return Ok(schedule);
    }

    // Изменение доступности слота
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchedule(
        int id,
        ScheduleUpdateDto dto)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound("Временной слот не найден");
        }

        // Нельзя открыть слот, если на него уже есть активная запись
        if (dto.IsAvailable)
        {
            var hasActiveBooking = await _context.Bookings
                .AnyAsync(b =>
                    b.ScheduleId == id &&
                    (b.Status == "Pending" || b.Status == "Confirmed"));

            if (hasActiveBooking)
            {
                return BadRequest(
                    "Нельзя открыть слот, на который уже существует активная запись");
            }
        }

        schedule.IsAvailable = dto.IsAvailable;

        await _context.SaveChangesAsync();

        return Ok(schedule);
    }

    // Удаление временного слота
    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound("Временной слот не найден");
        }

        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.ScheduleId == id);

        if (hasBookings)
        {
            return BadRequest(
                "Нельзя удалить временной слот, с которым связаны бронирования");
        }

        _context.Schedules.Remove(schedule);

        await _context.SaveChangesAsync();

        return Ok("Временной слот удалён");
    }
}