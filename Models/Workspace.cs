namespace TrelloCloneAPI.Models;

public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<Board> Boards { get; set; } = new List<Board>();
}
