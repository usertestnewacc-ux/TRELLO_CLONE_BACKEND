namespace TrelloCloneAPI.Models;

public class Board
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
    public ICollection<List> Lists { get; set; } = new List<List>();
}
