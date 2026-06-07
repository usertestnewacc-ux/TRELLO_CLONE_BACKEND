using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;
    private readonly TrelloDbContext _context;

    public BoardsController(IBoardService boardService, TrelloDbContext context)
    {
        _boardService = boardService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var id) ? id : null;
        var boards = await _boardService.GetAllAsync(userId);
        return Ok(boards);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var board = await _boardService.GetByIdAsync(id);
        if (board is null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var isAuthorized = board.CreatedById == userId ||
                               await _context.BoardMembers.AnyAsync(m => m.BoardId == id && m.UserId == userId) ||
                               await _context.Workspaces.AnyAsync(w => w.Id == board.WorkspaceId && 
                                   (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)));
                               
            if (!isAuthorized)
            {
                return Forbid();
            }
        }

        return Ok(board);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBoardDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
            dto = dto with { CreatedById = userId };

        var board = await _boardService.CreateAsync(dto);

        if (Guid.TryParse(userIdStr, out var uId))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = uId,
                Action = $"Created board '{board.Name}'",
                EntityType = "Board",
                EntityId = board.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(Get), new { id = board.Id }, board);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBoardDto dto)
    {
        var board = await _boardService.UpdateAsync(id, dto);
        return board is null ? NotFound() : Ok(board);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var board = await _boardService.GetByIdAsync(id);
        var deleted = await _boardService.DeleteAsync(id);
        if (!deleted) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId) && board is not null)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"Deleted board '{board.Name}'",
                EntityType = "Board",
                EntityId = board.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("members")]
    public async Task<IActionResult> AddMember(CreateBoardMemberDto dto)
    {
        var member = await _boardService.AddMemberAsync(dto);
        return CreatedAtAction(nameof(AddMember), new { id = member.Id }, member);
    }
}
