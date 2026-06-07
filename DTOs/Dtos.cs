using Microsoft.AspNetCore.Http;

namespace TrelloCloneAPI.DTOs;

public record UserDto(Guid Id, string Email, string? FirstName, string? LastName, Guid? RoleId, string? Status, DateTime CreatedAt);
public record CreateUserDto(string Email, string Password, string? FirstName, string? LastName, Guid? RoleId);
public record UpdateUserDto(string? FirstName, string? LastName, Guid? RoleId, string? Status);

public record RoleDto(Guid Id, string RoleName);
public record CreateRoleDto(string RoleName);

public record WorkspaceDto(Guid Id, string Name, string? Description, Guid OwnerId, DateTime CreatedAt);
public record CreateWorkspaceDto(string Name, string? Description, Guid OwnerId);
public record UpdateWorkspaceDto(string? Name, string? Description);

public record WorkspaceMemberDto(Guid Id, Guid WorkspaceId, Guid UserId, string? Role);
public record CreateWorkspaceMemberDto(Guid WorkspaceId, Guid UserId, string? Role);

public record BoardDto(Guid Id, Guid WorkspaceId, string WorkspaceName, string Name, string? Description, Guid CreatedById, DateTime CreatedAt);
public record CreateBoardDto(Guid WorkspaceId, string Name, string? Description, Guid CreatedById);
public record UpdateBoardDto(string? Name, string? Description);

public record BoardMemberDto(Guid Id, Guid BoardId, Guid UserId, string? Role);
public record CreateBoardMemberDto(Guid BoardId, Guid UserId, string? Role);

public record ListDto(Guid Id, Guid BoardId, string Title, int Position);
public record CreateListDto(Guid BoardId, string Title, int Position);
public record UpdateListDto(string? Title, int? Position);

public record CardDto(Guid Id, Guid ListId, string Title, string? Description, string? Priority, DateTime? DueDate, Guid? AssignedUserId, int Position, string? Status);
public record CreateCardDto(Guid ListId, string Title, string? Description, string? Priority, DateTime? DueDate, Guid? AssignedUserId, int Position, string? Status);
public record UpdateCardDto(Guid? ListId, string? Title, string? Description, string? Priority, DateTime? DueDate, Guid? AssignedUserId, int? Position, string? Status);

public record CommentDto(Guid Id, Guid CardId, Guid UserId, string CommentText, DateTime CreatedAt);
public record CreateCommentDto(Guid CardId, Guid UserId, string CommentText);

public record AttachmentDto(Guid Id, Guid CardId, string FileName, string FilePath, DateTime UploadedAt);
public record CreateAttachmentDto(Guid CardId, string FileName, string FilePath);
public record UploadAttachmentDto(Guid CardId, IFormFile File);

public record ActivityLogDto(Guid Id, Guid UserId, string Action, string EntityType, string EntityId, DateTime CreatedAt);
public record CreateActivityLogDto(Guid UserId, string Action, string EntityType, string EntityId);

public record ChartSegmentDto(string Label, int Value);
public record DashboardReportDto(
    int TotalWorkspaces,
    int TotalBoards,
    int TotalLists,
    int TotalCards,
    double AverageCardsPerList,
    int CompletedTasks,
    int InProgressTasks,
    int ToDoTasks,
    int OverdueTasks,
    int HighPriorityTasks,
    int MediumPriorityTasks,
    int LowPriorityTasks,
    IEnumerable<ChartSegmentDto> StatusBreakdown,
    IEnumerable<ChartSegmentDto> PriorityBreakdown,
    IEnumerable<ChartSegmentDto> AssigneeBreakdown);
