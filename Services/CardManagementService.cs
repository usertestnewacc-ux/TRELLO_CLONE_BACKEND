using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface ICardManagementService
{
    Task<IEnumerable<CardDto>> GetByListIdAsync(Guid listId);
    Task<IEnumerable<CardDto>> ReorderAsync(ReorderCardsDto dto);
}

public class CardManagementService : ICardManagementService
{
    private readonly TrelloDbContext _context;

    public CardManagementService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CardDto>> GetByListIdAsync(Guid listId)
    {
        return await _context.Cards
            .Where(c => c.ListId == listId)
            .OrderBy(c => c.Position)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<IEnumerable<CardDto>> ReorderAsync(ReorderCardsDto dto)
    {
        var cards = await _context.Cards
            .Where(c => c.ListId == dto.ListId)
            .ToListAsync();

        if (!cards.Any())
        {
            return Enumerable.Empty<CardDto>();
        }

        foreach (var item in dto.Items)
        {
            var card = cards.FirstOrDefault(c => c.Id == item.CardId);
            if (card is not null)
            {
                card.ListId = item.ListId;
                card.Position = item.Position;
            }
        }

        await _context.SaveChangesAsync();
        return cards.OrderBy(c => c.Position).Select(c => ToDto(c));
    }

    private static CardDto ToDto(Card card)
        => new(card.Id, card.ListId, card.Title, card.Description, card.Priority, card.DueDate, card.AssignedUserId, card.Position, card.Status);
}
