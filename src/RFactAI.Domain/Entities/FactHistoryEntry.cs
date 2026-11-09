namespace RFactAI.Domain.Entities;

/// <summary>One saved fact in a user's history.</summary>
public class FactHistoryEntry
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    /// <summary>"llm" or "fallback".</summary>
    public string Source { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
