
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
public class ServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public ServicesController(
        ApplicationDbContext context,
        IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // Получение всех активных услуг
    [HttpGet]
    public async Task<IActionResult> GetServices()
    {
        const string cacheKey = "active_services";

        if (!_cache.TryGetValue(
            cacheKey, out List<Service>? services) ||
            services == null)
        {
            services = await _context.Services
                .Where(s => s.IsActive)
                .ToListAsync();

            _cache.Set(
                cacheKey,
                services,
                TimeSpan.FromMinutes(5));
        }

        return Ok(services);
    }

    // Получение услуги по ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetService(int id)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        if (service == null)
        {
            return NotFound(new
            {
                message = "Услуга не найдена"
            });
        }

        return Ok(service);
    }

    // Добавление услуги
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateService(ServiceDto model)
    {
        var service = new Service
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            ImageUrl = model.ImageUrl
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        _cache.Remove("active_services");

        return CreatedAtAction(
            nameof(GetService),
            new { id = service.Id },
            service
        );
    }

    // Редактирование услуги
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateService(
        int id,
        ServiceDto model)
    {
        var service = await _context.Services.FindAsync(id);

        if (service == null)
        {
            return NotFound(new
            {
                message = "Услуга не найдена"
            });
        }

        service.Name = model.Name;
        service.Description = model.Description;
        service.Price = model.Price;
        service.ImageUrl = model.ImageUrl;

        await _context.SaveChangesAsync();

        _cache.Remove("active_services");

        return Ok(service);
    }

    // Удаление услуги
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _context.Services.FindAsync(id);

        if (service == null)
        {
            return NotFound(new
            {
                message = "Услуга не найдена"
            });
        }

        service.IsActive = false;

        await _context.SaveChangesAsync();

        _cache.Remove("active_services");

        return Ok(new
        {
            message = "Услуга успешно удалена"
        });
    }
}
