using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    private readonly TrelloDbContext _context;

    public CardsController(ICardService cardService, TrelloDbContext context)
    {
        _cardService = cardService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cards = await _cardService.GetAllAsync();
        return Ok(cards);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var card = await _cardService.GetByIdAsync(id);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCardDto dto)
    {
        var card = await _cardService.CreateAsync(dto);

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"Created card '{card.Title}'",
                EntityType = "Card",
                EntityId = card.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(Get), new { id = card.Id }, card);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCardDto dto)
    {
        var card = await _cardService.UpdateAsync(id, dto);
        if (card is null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"Updated card '{card.Title}'",
                EntityType = "Card",
                EntityId = card.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return Ok(card);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var card = await _cardService.GetByIdAsync(id);
        var deleted = await _cardService.DeleteAsync(id);
        if (!deleted) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId) && card is not null)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"Deleted card '{card.Title}'",
                EntityType = "Card",
                EntityId = card.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}
