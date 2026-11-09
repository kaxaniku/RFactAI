using Microsoft.Extensions.AI;
using NSubstitute;

namespace RFactAI.Application.Tests;

internal static class TestHelpers
{
    public static ChatResponse Reply(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    public static string FactsJson(string category, params string[] facts) =>
        System.Text.Json.JsonSerializer.Serialize(new { category, facts });

    /// <summary>Returns the replies in order; the last one repeats once the queue is exhausted.</summary>
    public static IChatClient ClientReturning(params string[] replies)
    {
        var client = Substitute.For<IChatClient>();
        var queue = new Queue<string>(replies);
        client.GetResponseAsync(default!, default, default)
            .ReturnsForAnyArgs(_ => Task.FromResult(Reply(queue.Count > 1 ? queue.Dequeue() : queue.Peek())));
        return client;
    }

    public static IChatClient ClientThrowing(Exception ex)
    {
        var client = Substitute.For<IChatClient>();
        client.GetResponseAsync(default!, default, default)
            .ReturnsForAnyArgs<Task<ChatResponse>>(_ => throw ex);
        return client;
    }
}
