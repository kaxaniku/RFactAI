using RFactAI.Application.Facts;

namespace RFactAI.Application.History;

public interface IHistoryService
{
    /// <summary>Saves each fact as a history row, then keeps only the newest 100 per user.</summary>
    Task RecordAsync(Guid userId, FactResponse response, FactSource source, CancellationToken cancellationToken = default);

    /// <summary>The saved facts for the user, newest first.</summary>
    Task<IReadOnlyList<HistoryItem>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
}
