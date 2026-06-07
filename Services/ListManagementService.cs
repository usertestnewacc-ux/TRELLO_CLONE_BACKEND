using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IListManagementService
{
    Task<IEnumerable<ListDto>> GetByBoardIdAsync(Guid boardId);
    Task<ListDto> CreateAsync(CreateListDto dto);
    Task<IEnumerable<ListDto>> ReorderAsync(ReorderListsDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class ListManagementService : IListManagementService
{
    private readonly TrelloDbContext _context;

    public ListManagementService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ListDto>> GetByBoardIdAsync(Guid boardId)
    {
        return await _context.Lists
            .Where(l => l.BoardId == boardId)
            .OrderBy(l => l.Position)
            .Select(l => new ListDto(l.Id, l.BoardId, l.Title, l.Position))
            .ToListAsync();
    }

    public async Task<ListDto> CreateAsync(CreateListDto dto)
    {
        var list = new List
        {
            Id = Guid.NewGuid(),
            BoardId = dto.BoardId,
            Title = dto.Title.Trim(),
            Position = dto.Position
        };

        _context.Lists.Add(list);
        await _context.SaveChangesAsync();
        return new ListDto(list.Id, list.BoardId, list.Title, list.Position);
    }

    public async Task<IEnumerable<ListDto>> ReorderAsync(ReorderListsDto dto)
    {
        var boardLists = await _context.Lists
            .Where(l => l.BoardId == dto.BoardId)
            .ToListAsync();

        if (!boardLists.Any())
        {
            return Enumerable.Empty<ListDto>();
        }

        foreach (var item in dto.Items)
        {
            var list = boardLists.FirstOrDefault(l => l.Id == item.ListId);
            if (list is not null)
            {
                list.Position = item.Position;
            }
        }

        await _context.SaveChangesAsync();

        return boardLists
            .OrderBy(l => l.Position)
            .Select(l => new ListDto(l.Id, l.BoardId, l.Title, l.Position));
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var list = await _context.Lists.FindAsync(id);
        if (list is null) return false;

        _context.Lists.Remove(list);
        await _context.SaveChangesAsync();
        return true;
    }
}
