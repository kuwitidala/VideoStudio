using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStudio.Data;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

// MVC-контроллер страниц. Не путать с API-контроллерами (/api/...):
// те отдают JSON, а этот возвращает готовые HTML-страницы (Views/Home/*.cshtml).
[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            Services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .Take(4)
                .ToListAsync(),

            Works = await _context.PortfolioItems
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync(),

            Reviews = await _context.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .ToListAsync(),

            Booking = await CatalogController.LoadBookingFormAsync(_context, null)
        };

        return View(model);
    }

    [HttpGet("about")]
    public IActionResult About() => View();

    [HttpGet("team")]
    public IActionResult Team() => View();

    [HttpGet("contacts")]
    public IActionResult Contacts() => View();

    // Вход и кабинет работают через JWT в браузере, поэтому страница — оболочка,
    // а содержимое рисует js/account.js
    [HttpGet("account")]
    public IActionResult Account() => View();

    [HttpGet("admin")]
    public IActionResult Admin() => View();
}
