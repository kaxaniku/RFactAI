using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using RFactAI.Domain.Exceptions;

namespace RFactAI.Application.Facts;

public sealed class FactService(
    IChatClient chatClient,
    IFallbackFactProvider fallbackProvider,
    ILogger<FactService> logger) : IFactService
{
    private const int MaxAttempts = 2;

    private static readonly ChatOptions Options = new()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema<FactResponse>(),
        Temperature = 0.8f
    };

    public async Task<FactResult> GetFactsAsync(string category, int count, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GenerateAsync(category, count, cancellationToken);
            return new FactResult(response, FactSource.Llm);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Covers: Ollama offline (HttpRequestException), timeout (TaskCanceledException),
            // model errors, and invalid JSON after the retry. Caller cancellation is never swallowed.
            logger.LogWarning(ex, "LLM fact generation failed for category {Category}; trying fallback.", category);

            var fallback = fallbackProvider.Get(category, count);
            if (fallback is null)
                throw new LlmUnavailableException(
                    $"Fact generation is unavailable and no fallback facts exist for category '{category}'.", ex);

            return new FactResult(fallback, FactSource.Fallback);
        }
    }

    private async Task<FactResponse> GenerateAsync(string category, int count, CancellationToken ct)
    {
        var messages = FactPrompt.Create(category, count);
        InvalidLlmResponseException? lastInvalid = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var reply = await chatClient.GetResponseAsync(messages, Options, ct);
            try
            {
                return FactJsonParser.Parse(reply.Text, category, count);
            }
            catch (InvalidLlmResponseException ex)
            {
                lastInvalid = ex;
                logger.LogWarning(ex, "Invalid LLM JSON for category {Category} (attempt {Attempt}/{Max}).",
                    category, attempt, MaxAttempts);
                messages.Add(new ChatMessage(ChatRole.Assistant, reply.Text));
                messages.Add(new ChatMessage(ChatRole.User, FactPrompt.RetryNudge));
            }
        }

        throw lastInvalid!;
    }
}
