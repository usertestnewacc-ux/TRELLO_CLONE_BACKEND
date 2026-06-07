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
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly TrelloDbContext _context;

    public WorkspacesController(IWorkspaceService workspaceService, TrelloDbContext context)
    {
        _workspaceService = workspaceService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdStr, out var id) ? id : null;
        var workspaces = await _workspaceService.GetAllAsync(userId);
        return Ok(workspaces);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var workspace = await _workspaceService.GetByIdAsync(id);
        if (workspace is null) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var isMember = await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == id && m.UserId == userId);
            if (workspace.OwnerId != userId && !isMember)
            {
                return Forbid();
            }
        }

        return Ok(workspace);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkspaceDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
            dto = dto with { OwnerId = userId };

        var workspace = await _workspaceService.CreateAsync(dto);

        if (Guid.TryParse(userIdStr, out var uId))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = uId,
                Action = $"Created workspace '{workspace.Name}'",
                EntityType = "Workspace",
                EntityId = workspace.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(Get), new { id = workspace.Id }, workspace);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkspaceDto dto)
    {
        var workspace = await _workspaceService.UpdateAsync(id, dto);
        return workspace is null ? NotFound() : Ok(workspace);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workspace = await _workspaceService.GetByIdAsync(id);
        var deleted = await _workspaceService.DeleteAsync(id);
        if (!deleted) return NotFound();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId) && workspace is not null)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = $"Deleted workspace '{workspace.Name}'",
                EntityType = "Workspace",
                EntityId = workspace.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("members")]
    public async Task<IActionResult> AddMember(CreateWorkspaceMemberDto dto)
    {
        var member = await _workspaceService.AddMemberAsync(dto);
        return CreatedAtAction(nameof(AddMember), new { id = member.Id }, member);
    }
}

