using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStudio.Data;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

// Страницы, которые выводят данные из БД: услуги, портфолио, отзывы, запись
[ApiExplorerSettings(IgnoreApi = true)]
public class CatalogController : Controller
{
    private readonly ApplicationDbContext _context;

    public CatalogController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("services")]
    public async Task<IActionResult> Services()
    {
        var services = await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync();

        return View(services);
    }

    [HttpGet("portfolio")]
    public async Task<IActionResult> Portfolio()
    {
        var works = await _context.PortfolioItems
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(works);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews()
    {
        var reviews = await _context.Reviews
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(reviews);
    }

    // /booking?service=3 — услуга выбрана заранее
    [HttpGet("booking")]
    public async Task<IActionResult> Booking(int? service)
    {
        return View(await LoadBookingFormAsync(_context, service));
    }

    // Общий метод: его же использует главная страница
    public static async Task<BookingFormViewModel> LoadBookingFormAsync(
        ApplicationDbContext context,
        int? selectedServiceId)
    {
        var services = await context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var slots = await context.Schedules
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.StartAt)
            .ToListAsync();

        return new BookingFormViewModel
        {
            Services = services,
            // будущие слоты отбираем уже в памяти
            Slots = slots.Where(s => s.StartAt > DateTime.UtcNow).ToList(),
            SelectedServiceId = services.Any(s => s.Id == selectedServiceId)
                ? selectedServiceId
                : null
        };
    }
}
