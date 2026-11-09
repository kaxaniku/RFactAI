using Microsoft.EntityFrameworkCore;
using RFactAI.Domain.Entities;

namespace RFactAI.Application.Abstractions;

/// <summary>The slice of the EF Core context the application needs. Implemented by Infrastructure's AppDbContext.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<FactHistoryEntry> History { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
