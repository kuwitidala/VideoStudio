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
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Получение опубликованных отзывов
    [HttpGet]
    public async Task<IActionResult> GetReviews()
    {
        var reviews = await _context.Reviews
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews);
    }

    // Добавление отзыва
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateReview(ReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return BadRequest("Оценка должна быть от 1 до 5");
        }

        if (string.IsNullOrWhiteSpace(dto.Text))
        {
            return BadRequest("Текст отзыва не может быть пустым");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var review = new Review
        {
            UserId = userId!,
            Rating = dto.Rating,
            Text = dto.Text,
            IsApproved = false
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok("Отзыв отправлен на модерацию");
    }

    // Просмотр всех отзывов для менеджера и администратора
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllReviews()
    {
        var reviews = await _context.Reviews
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews);
    }

    // Изменение статуса публикации
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
        {
            return NotFound("Отзыв не найден");
        }

        review.IsApproved = true;

        await _context.SaveChangesAsync();

        return Ok("Отзыв опубликован");
    }

    // Удаление отзыва
    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
        {
            return NotFound("Отзыв не найден");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok("Отзыв удалён");
    }
}