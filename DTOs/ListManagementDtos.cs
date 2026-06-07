namespace TrelloCloneAPI.DTOs;

public record ReorderListItemDto(Guid ListId, int Position);
public record ReorderListsDto(Guid BoardId, IEnumerable<ReorderListItemDto> Items);
