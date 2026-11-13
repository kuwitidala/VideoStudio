
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VideoStudio.Data;
using VideoStudio.DTOs;
using VideoStudio.Models;

namespace VideoStudio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public PortfolioController(
        ApplicationDbContext context,
        IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // Получение всех работ
    [HttpGet]
    public async Task<IActionResult> GetPortfolio()
    {
        const string cacheKey = "portfolio_items";

        if (!_cache.TryGetValue(
            cacheKey, out List<PortfolioItem>? items) ||
            items == null)
        {
            items = await _context.PortfolioItems
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _cache.Set(
                cacheKey,
                items,
                TimeSpan.FromMinutes(5));
        }

        return Ok(items);
    }

    // Получение работы по ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPortfolioItem(int id)
    {
        var item = await _context.PortfolioItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                message = "Работа не найдена"
            });
        }

        return Ok(item);
    }

    // Добавление работы
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePortfolioItem(
        PortfolioDto model)
    {
        var item = new PortfolioItem
        {
            Title = model.Title,
            Description = model.Description,
            ImageUrl = model.ImageUrl,
            VideoUrl = model.VideoUrl
        };

        _context.PortfolioItems.Add(item);
        await _context.SaveChangesAsync();

        _cache.Remove("portfolio_items");

        return CreatedAtAction(
            nameof(GetPortfolioItem),
            new { id = item.Id },
            item
        );
    }

    // Редактирование работы
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePortfolioItem(
        int id,
        PortfolioDto model)
    {
        var item = await _context.PortfolioItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                message = "Работа не найдена"
            });
        }

        item.Title = model.Title;
        item.Description = model.Description;
        item.ImageUrl = model.ImageUrl;
        item.VideoUrl = model.VideoUrl;

        await _context.SaveChangesAsync();

        _cache.Remove("portfolio_items");

        return Ok(item);
    }

    // Удаление работы
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePortfolioItem(int id)
    {
        var item = await _context.PortfolioItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new
            {
                message = "Работа не найдена"
            });
        }

        _context.PortfolioItems.Remove(item);
        await _context.SaveChangesAsync();

        _cache.Remove("portfolio_items");

        return Ok(new
        {
            message = "Работа успешно удалена"
        });
    }
}
