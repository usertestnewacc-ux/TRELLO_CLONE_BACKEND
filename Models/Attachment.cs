namespace TrelloCloneAPI.Models;

public class Attachment
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public Card? Card { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
