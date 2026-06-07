using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetAllAsync();
    Task<IEnumerable<CommentDto>> GetByCardIdAsync(Guid cardId);
    Task<CommentDto?> GetByIdAsync(Guid id);
    Task<CommentDto> CreateAsync(CreateCommentDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class CommentService : ICommentService
{
    private readonly TrelloDbContext _context;

    public CommentService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CommentDto>> GetAllAsync()
    {
        return await _context.Comments
            .Select(c => new CommentDto(c.Id, c.CardId, c.UserId, c.CommentText, c.CreatedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<CommentDto>> GetByCardIdAsync(Guid cardId)
    {
        return await _context.Comments
            .Where(c => c.CardId == cardId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.CardId, c.UserId, c.CommentText, c.CreatedAt))
            .ToListAsync();
    }

    public async Task<CommentDto?> GetByIdAsync(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        return comment is null ? null : new CommentDto(comment.Id, comment.CardId, comment.UserId, comment.CommentText, comment.CreatedAt);
    }

    public async Task<CommentDto> CreateAsync(CreateCommentDto dto)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            CardId = dto.CardId,
            UserId = dto.UserId,
            CommentText = dto.CommentText,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return new CommentDto(comment.Id, comment.CardId, comment.UserId, comment.CommentText, comment.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment is null) return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }
}
