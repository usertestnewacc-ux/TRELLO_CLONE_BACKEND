namespace TrelloCloneAPI.Models;

public class Card
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public List? List { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public int Position { get; set; }
    public string? Status { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
