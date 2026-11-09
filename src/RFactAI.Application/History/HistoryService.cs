using Microsoft.EntityFrameworkCore;
using RFactAI.Application.Abstractions;
using RFactAI.Application.Facts;
using RFactAI.Domain;
using RFactAI.Domain.Entities;

namespace RFactAI.Application.History;

public sealed class HistoryService(IAppDbContext db) : IHistoryService
{
    public async Task RecordAsync(Guid userId, FactResponse response, FactSource source, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sourceName = source.ToString().ToLowerInvariant();
        var newEntries = response.Facts
            .Select(fact => new FactHistoryEntry
            {
                UserId = userId,
                Category = response.Category,
                Text = fact,
                Source = sourceName,
                CreatedAt = now
            })
            .ToList();

        // Evict the oldest rows first so that existing + new never exceeds the cap.
        // Done in the same SaveChanges (one transaction) as the insert.
        var existing = await db.History.CountAsync(h => h.UserId == userId, cancellationToken);
        var overflow = existing + newEntries.Count - FactLimits.MaxHistoryPerUser;
        if (overflow > 0)
        {
            var oldest = await db.History
                .Where(h => h.UserId == userId)
                .OrderBy(h => h.Id)
                .Take(overflow)
                .ToListAsync(cancellationToken);
            db.History.RemoveRange(oldest);
        }

        db.History.AddRange(newEntries);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistoryItem>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.History
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt) // newest request first...
            .ThenBy(h => h.Id)                   // ...facts within a request in their original order
            .Take(FactLimits.MaxHistoryPerUser)
            .Select(h => new HistoryItem(h.Id, h.Category, h.Text, h.Source, h.CreatedAt))
            .ToListAsync(cancellationToken);
}
