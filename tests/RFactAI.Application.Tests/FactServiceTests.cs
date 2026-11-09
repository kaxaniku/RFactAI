using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RFactAI.Application.Facts;
using RFactAI.Domain.Exceptions;
using RFactAI.Infrastructure;
using static RFactAI.Application.Tests.TestHelpers;

namespace RFactAI.Application.Tests;

public class FactServiceTests
{
    private static FactService Create(IChatClient client) =>
        new(client, new StaticFallbackFactProvider(), NullLogger<FactService>.Instance);

    [Fact]
    public async Task Returns_llm_facts_for_valid_json()
    {
        var client = ClientReturning(FactsJson("space", "A", "B", "C"));

        var result = await Create(client).GetFactsAsync("space", 3);

        Assert.Equal(FactSource.Llm, result.Source);
        Assert.Equal("space", result.Response.Category);
        Assert.Equal(["A", "B", "C"], result.Response.Facts);
    }

    [Fact]
    public async Task Requests_json_schema_and_includes_category_and_count_in_prompt()
    {
        var client = ClientReturning(FactsJson("space", "A", "B"));

        await Create(client).GetFactsAsync("space", 2);

        var call = client.ReceivedCalls().Single();
        var messages = ((IEnumerable<ChatMessage>)call.GetArguments()[0]!).ToList();
        var options = (ChatOptions)call.GetArguments()[1]!;
        Assert.NotNull(options.ResponseFormat);
        Assert.Contains(messages, m => m.Role == ChatRole.User && m.Text.Contains("space") && m.Text.Contains("2"));
    }

    [Fact]
    public async Task Trims_when_model_returns_too_many_facts()
    {
        var client = ClientReturning(FactsJson("space", "A", "B", "C", "D"));

        var result = await Create(client).GetFactsAsync("space", 2);

        Assert.Equal(2, result.Response.Facts.Count);
    }

    [Fact]
    public async Task Accepts_fenced_json()
    {
        var client = ClientReturning("```json\n" + FactsJson("space", "A") + "\n```");

        var result = await Create(client).GetFactsAsync("space", 1);

        Assert.Equal(FactSource.Llm, result.Source);
    }

    [Fact]
    public async Task Retries_once_after_invalid_json_then_succeeds()
    {
        var client = ClientReturning("not json", FactsJson("space", "A"));

        var result = await Create(client).GetFactsAsync("space", 1);

        Assert.Equal(FactSource.Llm, result.Source);
        Assert.Equal(2, client.ReceivedCalls().Count());
    }

    [Fact]
    public async Task Falls_back_when_json_is_invalid_twice()
    {
        var client = ClientReturning("not json");

        var result = await Create(client).GetFactsAsync("space", 2);

        Assert.Equal(FactSource.Fallback, result.Source);
        Assert.Equal(2, result.Response.Facts.Count);
        Assert.Equal(2, client.ReceivedCalls().Count());
    }

    [Fact]
    public async Task Falls_back_when_ollama_is_offline()
    {
        var client = ClientThrowing(new HttpRequestException("Connection refused"));

        var result = await Create(client).GetFactsAsync("science", 3);

        Assert.Equal(FactSource.Fallback, result.Source);
        Assert.Equal(3, result.Response.Facts.Count);
    }

    [Fact]
    public async Task Falls_back_on_timeout()
    {
        var client = ClientThrowing(new TaskCanceledException("timeout"));

        var result = await Create(client).GetFactsAsync("history", 1);

        Assert.Equal(FactSource.Fallback, result.Source);
    }

    [Fact]
    public async Task Throws_unavailable_when_failing_and_category_has_no_fallback()
    {
        var client = ClientThrowing(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<LlmUnavailableException>(() => Create(client).GetFactsAsync("quantum-knitting", 3));
    }

    [Fact]
    public async Task Caller_cancellation_is_not_swallowed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var client = ClientThrowing(new OperationCanceledException(cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Create(client).GetFactsAsync("space", 1, cts.Token));
    }
}
