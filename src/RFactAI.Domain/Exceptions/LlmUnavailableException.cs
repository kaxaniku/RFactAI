namespace RFactAI.Domain.Exceptions;

/// <summary>The LLM could not produce facts and no fallback exists for the requested category.</summary>
public sealed class LlmUnavailableException : FactGenerationException
{
    public LlmUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
