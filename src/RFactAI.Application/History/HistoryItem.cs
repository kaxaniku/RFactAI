namespace RFactAI.Application.History;

public sealed record HistoryItem(long Id, string Category, string Fact, string Source, DateTime CreatedAt);
