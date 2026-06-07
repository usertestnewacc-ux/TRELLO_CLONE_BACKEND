using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IBoardService
{
    Task<IEnumerable<BoardDto>> GetAllAsync(Guid? userId = null);
    Task<BoardDto?> GetByIdAsync(Guid id);
    Task<BoardDto> CreateAsync(CreateBoardDto dto);
    Task<BoardDto?> UpdateAsync(Guid id, UpdateBoardDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<BoardMemberDto> AddMemberAsync(CreateBoardMemberDto dto);
}

public class BoardService : IBoardService
{
    private readonly TrelloDbContext _context;

    public BoardService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoardDto>> GetAllAsync(Guid? userId = null)
    {
        IQueryable<Board> query = _context.Boards;
        if (userId.HasValue)
        {
            query = query.Where(b => b.CreatedById == userId.Value || 
                                     b.Members.Any(m => m.UserId == userId.Value) ||
                                     b.Workspace.OwnerId == userId.Value ||
                                     b.Workspace.Members.Any(m => m.UserId == userId.Value));
        }
        return await query
            .Select(b => new BoardDto(b.Id, b.WorkspaceId, b.Workspace!.Name, b.Name, b.Description, b.CreatedById, b.CreatedAt))
            .ToListAsync();
    }

    public async Task<BoardDto?> GetByIdAsync(Guid id)
    {
        var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == id);
        return board is null ? null : new BoardDto(board.Id, board.WorkspaceId, board.Workspace!.Name, board.Name, board.Description, board.CreatedById, board.CreatedAt);
    }

    public async Task<BoardDto> CreateAsync(CreateBoardDto dto)
    {
        var board = new Board
        {
            Id = Guid.NewGuid(),
            WorkspaceId = dto.WorkspaceId,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            CreatedById = dto.CreatedById,
            CreatedAt = DateTime.UtcNow
        };

        _context.Boards.Add(board);
        await _context.SaveChangesAsync();
        
        var workspace = await _context.Workspaces.FindAsync(dto.WorkspaceId);
        return new BoardDto(board.Id, board.WorkspaceId, workspace?.Name ?? "Unknown Workspace", board.Name, board.Description, board.CreatedById, board.CreatedAt);
    }

    public async Task<BoardDto?> UpdateAsync(Guid id, UpdateBoardDto dto)
    {
        var board = await _context.Boards.Include(b => b.Workspace).FirstOrDefaultAsync(b => b.Id == id);
        if (board is null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name)) board.Name = dto.Name.Trim();
        if (dto.Description is not null) board.Description = dto.Description;

        await _context.SaveChangesAsync();
        return new BoardDto(board.Id, board.WorkspaceId, board.Workspace!.Name, board.Name, board.Description, board.CreatedById, board.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var board = await _context.Boards.FindAsync(id);
        if (board is null) return false;

        _context.Boards.Remove(board);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BoardMemberDto> AddMemberAsync(CreateBoardMemberDto dto)
    {
        var member = new BoardMember
        {
            Id = Guid.NewGuid(),
            BoardId = dto.BoardId,
            UserId = dto.UserId,
            Role = dto.Role
        };

        _context.BoardMembers.Add(member);
        await _context.SaveChangesAsync();
        return new BoardMemberDto(member.Id, member.BoardId, member.UserId, member.Role);
    }
}
