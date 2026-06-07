namespace TrelloCloneAPI.DTOs;

public record ReorderCardItemDto(Guid CardId, Guid ListId, int Position);
public record ReorderCardsDto(Guid ListId, IEnumerable<ReorderCardItemDto> Items);
