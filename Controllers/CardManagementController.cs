using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CardManagementController : ControllerBase
{
    private readonly ICardManagementService _cardManagementService;

    public CardManagementController(ICardManagementService cardManagementService)
    {
        _cardManagementService = cardManagementService;
    }

    [HttpGet("list/{listId:guid}")]
    public async Task<IActionResult> GetByListId(Guid listId)
    {
        var cards = await _cardManagementService.GetByListIdAsync(listId);
        return Ok(cards);
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(ReorderCardsDto dto)
    {
        var reordered = await _cardManagementService.ReorderAsync(dto);
        return Ok(reordered);
    }
}
