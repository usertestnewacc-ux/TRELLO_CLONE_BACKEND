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
public class BoardMembersController : ControllerBase
{
    private readonly TrelloDbContext _context;

    public BoardMembersController(TrelloDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid boardId)
    {
        if (boardId == Guid.Empty)
        {
            return BadRequest(new { error = "The boardId query parameter is required." });
        }

        var members = await _context.BoardMembers
            .Include(bm => bm.User)
            .Where(bm => bm.BoardId == boardId)
            .Select(bm => new BoardMemberDetailDto(
                bm.Id,
                bm.BoardId,
                bm.UserId,
                bm.User != null ? bm.User.Email : null,
                bm.Role))
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite(InviteBoardMemberDto dto)
    {
        if (dto.BoardId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(new { error = "BoardId, email, and role are required." });
        }

        var board = await _context.Boards.FindAsync(dto.BoardId);
        if (board is null)
        {
            return NotFound(new { error = "Board not found." });
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            return NotFound(new { error = "User not found." });
        }

        var existingMember = await _context.BoardMembers
            .AnyAsync(bm => bm.BoardId == dto.BoardId && bm.UserId == user.Id);
        if (existingMember)
        {
            return Conflict(new { error = "User is already a member of this board." });
        }

        var member = new BoardMember
        {
            Id = Guid.NewGuid(),
            BoardId = dto.BoardId,
            UserId = user.Id,
            Role = dto.Role.Trim()
        };

        _context.BoardMembers.Add(member);

        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserIdStr, out var currentUserId))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = $"Invited member '{user.Email}' to board",
                EntityType = "Board",
                EntityId = dto.BoardId.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { boardId = dto.BoardId },
            new BoardMemberDetailDto(member.Id, member.BoardId, member.UserId, user.Email, member.Role));
    }

    [HttpPut("{memberId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid memberId, UpdateBoardMemberRoleDto dto)
    {
        if (memberId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Role))
        {
            return BadRequest(new { error = "MemberId and role are required." });
        }

        var member = await _context.BoardMembers.FindAsync(memberId);
        if (member is null)
        {
            return NotFound(new { error = "Board member not found." });
        }

        member.Role = dto.Role.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { member.Id, member.BoardId, member.UserId, member.Role });
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Delete(Guid memberId)
    {
        var member = await _context.BoardMembers.FindAsync(memberId);
        if (member is null)
        {
            return NotFound(new { error = "Board member not found." });
        }

        _context.BoardMembers.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
