using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface ICardService
{
    Task<IEnumerable<CardDto>> GetAllAsync();
    Task<CardDto?> GetByIdAsync(Guid id);
    Task<CardDto> CreateAsync(CreateCardDto dto);
    Task<CardDto?> UpdateAsync(Guid id, UpdateCardDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class CardService : ICardService
{
    private readonly TrelloDbContext _context;

    public CardService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CardDto>> GetAllAsync()
    {
        return await _context.Cards
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<CardDto?> GetByIdAsync(Guid id)
    {
        var card = await _context.Cards.FindAsync(id);
        return card is null ? null : ToDto(card);
    }

    public async Task<CardDto> CreateAsync(CreateCardDto dto)
    {
        var card = new Card
        {
            Id = Guid.NewGuid(),
            ListId = dto.ListId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            AssignedUserId = dto.AssignedUserId,
            Position = dto.Position,
            Status = dto.Status
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return ToDto(card);
    }

    public async Task<CardDto?> UpdateAsync(Guid id, UpdateCardDto dto)
    {
        var card = await _context.Cards.FindAsync(id);
        if (card is null) return null;

        if (dto.ListId.HasValue) card.ListId = dto.ListId.Value;
        if (!string.IsNullOrWhiteSpace(dto.Title)) card.Title = dto.Title.Trim();
        if (dto.Description is not null) card.Description = dto.Description;
        if (dto.Priority is not null) card.Priority = dto.Priority;
        if (dto.DueDate.HasValue) card.DueDate = dto.DueDate;
        if (dto.AssignedUserId.HasValue) card.AssignedUserId = dto.AssignedUserId;
        if (dto.Position.HasValue) card.Position = dto.Position.Value;
        if (dto.Status is not null) card.Status = dto.Status;

        await _context.SaveChangesAsync();
        return ToDto(card);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var card = await _context.Cards.FindAsync(id);
        if (card is null) return false;

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        return true;
    }

    private static CardDto ToDto(Card card)
        => new(card.Id, card.ListId, card.Title, card.Description, card.Priority, card.DueDate, card.AssignedUserId, card.Position, card.Status);
}
