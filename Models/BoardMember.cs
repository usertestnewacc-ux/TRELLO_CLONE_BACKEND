namespace TrelloCloneAPI.Models;

public class BoardMember
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Board? Board { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Role { get; set; }
}
