namespace TrelloCloneAPI.DTOs;

public record BoardMemberDetailDto(Guid Id, Guid BoardId, Guid UserId, string? Email, string? Role);
public record InviteBoardMemberDto(Guid BoardId, string Email, string Role);
public record UpdateBoardMemberRoleDto(string Role);
