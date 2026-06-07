using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var comments = await _commentService.GetAllAsync();
        return Ok(comments);
    }

    [HttpGet("card/{cardId:guid}")]
    public async Task<IActionResult> GetByCardId(Guid cardId)
    {
        var comments = await _commentService.GetByCardIdAsync(cardId);
        return Ok(comments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        return comment is null ? NotFound() : Ok(comment);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCommentDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
            dto = dto with { UserId = userId };

        var comment = await _commentService.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = comment.Id }, comment);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _commentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
