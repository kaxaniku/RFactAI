using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RFactAI.Infrastructure.Persistence;

namespace RFactAI.Application.Tests;

internal static class TestApi
{
    public const string SigningKey = "test-signing-key-test-signing-key-1234567890";

    /// <summary>The real app with the chat client mocked and PostgreSQL swapped for an isolated in-memory database.</summary>
    public static WebApplicationFactory<Program> CreateFactory(IChatClient chat) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            var dbName = Guid.NewGuid().ToString();
            b.UseSetting("Jwt:SigningKey", SigningKey);
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IChatClient>();
                s.AddSingleton(chat);

                s.RemoveAll<DbContextOptions<AppDbContext>>();
                s.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                s.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });

    public static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
