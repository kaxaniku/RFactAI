using Microsoft.EntityFrameworkCore;
using RFactAI.Application.Facts;
using RFactAI.Application.History;
using RFactAI.Domain;

namespace RFactAI.Application.Tests;

public class HistoryServiceTests
{
    private static async Task Fill(HistoryService history, Guid userId, int count, string prefix)
    {
        for (var i = 0; i < count; i++)
            await history.RecordAsync(userId, new FactResponse("space", [$"{prefix}-{i}"]), FactSource.Llm);
    }

    [Fact]
    public async Task Records_each_fact_with_category_and_source()
    {
        var db = TestApi.CreateDb();
        var history = new HistoryService(db);
        var userId = Guid.NewGuid();

        await history.RecordAsync(userId, new FactResponse("space", ["A", "B"]), FactSource.Fallback);

        var rows = await db.History.OrderBy(h => h.Id).ToListAsync();
        Assert.Equal(["A", "B"], rows.Select(r => r.Text));
        Assert.All(rows, r =>
        {
            Assert.Equal(userId, r.UserId);
            Assert.Equal("space", r.Category);
            Assert.Equal("fallback", r.Source);
        });
    }

    [Fact]
    public async Task Adding_beyond_the_cap_evicts_the_oldest()
    {
        var db = TestApi.CreateDb();
        var history = new HistoryService(db);
        var userId = Guid.NewGuid();

        await Fill(history, userId, FactLimits.MaxHistoryPerUser, "old");
        await history.RecordAsync(userId, new FactResponse("space", ["newest"]), FactSource.Llm);

        var texts = await db.History.Where(h => h.UserId == userId).Select(h => h.Text).ToListAsync();
        Assert.Equal(FactLimits.MaxHistoryPerUser, texts.Count);
        Assert.DoesNotContain("old-0", texts);
        Assert.Contains("old-1", texts);
        Assert.Contains("newest", texts);
    }

    [Fact]
    public async Task One_request_that_crosses_the_cap_trims_exactly_the_overflow()
    {
        var db = TestApi.CreateDb();
        var history = new HistoryService(db);
        var userId = Guid.NewGuid();
        await Fill(history, userId, 95, "old");

        var batch = Enumerable.Range(0, 10).Select(i => $"new-{i}").ToList();
        await history.RecordAsync(userId, new FactResponse("space", batch), FactSource.Llm);

        var texts = await db.History.Where(h => h.UserId == userId).Select(h => h.Text).ToListAsync();
        Assert.Equal(FactLimits.MaxHistoryPerUser, texts.Count);
        Assert.DoesNotContain("old-4", texts);
        Assert.Contains("old-5", texts);
        Assert.All(batch, n => Assert.Contains(n, texts));
    }

    [Fact]
    public async Task Users_do_not_affect_each_others_history()
    {
        var db = TestApi.CreateDb();
        var history = new HistoryService(db);
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        await Fill(history, alice, FactLimits.MaxHistoryPerUser, "alice");
        await history.RecordAsync(bob, new FactResponse("space", ["bob-1"]), FactSource.Llm);

        Assert.Equal(FactLimits.MaxHistoryPerUser, await db.History.CountAsync(h => h.UserId == alice));
        Assert.Equal(1, await db.History.CountAsync(h => h.UserId == bob));
        Assert.Single(await history.ListAsync(bob));
    }

    [Fact]
    public async Task List_returns_newest_request_first_keeping_fact_order_within_a_request()
    {
        var db = TestApi.CreateDb();
        var history = new HistoryService(db);
        var userId = Guid.NewGuid();

        await history.RecordAsync(userId, new FactResponse("space", ["first-1", "first-2"]), FactSource.Llm);
        await Task.Delay(20); // distinct timestamps for the two requests
        await history.RecordAsync(userId, new FactResponse("space", ["second-1", "second-2"]), FactSource.Llm);

        var items = await history.ListAsync(userId);

        Assert.Equal(["second-1", "second-2", "first-1", "first-2"], items.Select(i => i.Fact));
    }
}
