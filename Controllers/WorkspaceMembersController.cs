using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkspaceMembersController : ControllerBase
{
    private readonly TrelloDbContext _context;

    public WorkspaceMembersController(TrelloDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(new { error = "The workspaceId query parameter is required." });
        }

        var members = await _context.WorkspaceMembers
            .Include(wm => wm.User)
            .Where(wm => wm.WorkspaceId == workspaceId)
                .Select(wm => new WorkspaceMemberDetailDto(
                    wm.Id,
                    wm.WorkspaceId,
                    wm.UserId,
                    wm.User != null ? wm.User.Email : null,
                    wm.Role))
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite(InviteWorkspaceMemberDto dto)
    {
        if (dto.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(new { error = "WorkspaceId, email, and role are required." });
        }

        var workspace = await _context.Workspaces.FindAsync(dto.WorkspaceId);
        if (workspace is null)
        {
            return NotFound(new { error = "Workspace not found." });
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            return NotFound(new { error = "User not found." });
        }

        var existingMember = await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == dto.WorkspaceId && wm.UserId == user.Id);
        if (existingMember)
        {
            return Conflict(new { error = "User is already a member of this workspace." });
        }

        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = dto.WorkspaceId,
            UserId = user.Id,
            Role = dto.Role.Trim()
        };

        _context.WorkspaceMembers.Add(member);

        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserIdStr, out var currentUserId))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = $"Invited member '{user.Email}' to workspace",
                EntityType = "Workspace",
                EntityId = dto.WorkspaceId.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { workspaceId = dto.WorkspaceId },
            new WorkspaceMemberDetailDto(member.Id, member.WorkspaceId, member.UserId, user.Email, member.Role));
    }

    [HttpPut("{memberId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid memberId, UpdateWorkspaceMemberRoleDto dto)
    {
        if (memberId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(new { error = "MemberId and role are required." });
        }

        var member = await _context.WorkspaceMembers.FindAsync(memberId);
        if (member is null)
        {
            return NotFound(new { error = "Workspace member not found." });
        }

        member.Role = dto.Role.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { member.Id, member.WorkspaceId, member.UserId, member.Role });
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Delete(Guid memberId)
    {
        var member = await _context.WorkspaceMembers.FindAsync(memberId);
        if (member is null)
        {
            return NotFound(new { error = "Workspace member not found." });
        }

        _context.WorkspaceMembers.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
