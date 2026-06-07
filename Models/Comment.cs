namespace TrelloCloneAPI.Models;

public class Comment
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string CommentText { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
