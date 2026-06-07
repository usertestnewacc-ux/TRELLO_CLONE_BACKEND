namespace TrelloCloneAPI.Models;

public class WorkspaceMember
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Role { get; set; }
}
