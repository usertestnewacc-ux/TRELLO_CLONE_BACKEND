namespace TrelloCloneAPI.Models;

public class List
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Board? Board { get; set; }
    public string Title { get; set; } = null!;
    public int Position { get; set; }

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
