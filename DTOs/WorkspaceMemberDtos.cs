namespace TrelloCloneAPI.DTOs;

public record WorkspaceMemberDetailDto(Guid Id, Guid WorkspaceId, Guid UserId, string? Email, string? Role);
public record InviteWorkspaceMemberDto(Guid WorkspaceId, string Email, string Role);
public record UpdateWorkspaceMemberRoleDto(string Role);
