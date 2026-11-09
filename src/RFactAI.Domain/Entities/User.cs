namespace RFactAI.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stored trimmed and lower-cased; unique.</summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<FactHistoryEntry> History { get; set; } = [];
}
