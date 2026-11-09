using Microsoft.EntityFrameworkCore;
using RFactAI.Application.Abstractions;
using RFactAI.Domain;
using RFactAI.Domain.Entities;

namespace RFactAI.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<FactHistoryEntry> History => Set<FactHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(FactLimits.MaxEmailLength).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<FactHistoryEntry>(e =>
        {
            e.ToTable("FactHistory");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedOnAdd();
            e.Property(h => h.Category).HasMaxLength(FactLimits.MaxCategoryLength).IsRequired();
            e.Property(h => h.Text).HasMaxLength(2000).IsRequired();
            e.Property(h => h.Source).HasMaxLength(20).IsRequired();
            e.HasOne(h => h.User)
                .WithMany(u => u.History)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => new { h.UserId, h.Id });
        });
    }
}
