using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IListService
{
    Task<IEnumerable<ListDto>> GetAllAsync();
    Task<ListDto?> GetByIdAsync(Guid id);
    Task<ListDto> CreateAsync(CreateListDto dto);
    Task<ListDto?> UpdateAsync(Guid id, UpdateListDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class ListService : IListService
{
    private readonly TrelloDbContext _context;

    public ListService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ListDto>> GetAllAsync()
    {
        return await _context.Lists
            .Select(l => new ListDto(l.Id, l.BoardId, l.Title, l.Position))
            .ToListAsync();
    }

    public async Task<ListDto?> GetByIdAsync(Guid id)
    {
        var list = await _context.Lists.FindAsync(id);
        return list is null ? null : new ListDto(list.Id, list.BoardId, list.Title, list.Position);
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

    public async Task<ListDto?> UpdateAsync(Guid id, UpdateListDto dto)
    {
        var list = await _context.Lists.FindAsync(id);
        if (list is null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title)) list.Title = dto.Title.Trim();
        if (dto.Position.HasValue) list.Position = dto.Position.Value;

        await _context.SaveChangesAsync();
        return new ListDto(list.Id, list.BoardId, list.Title, list.Position);
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
