namespace RFactAI.Domain.Exceptions;

/// <summary>The LLM replied, but not with the JSON shape we asked for.</summary>
public sealed class InvalidLlmResponseException : FactGenerationException
{
    public InvalidLlmResponseException(string message, Exception? inner = null) : base(message, inner) { }
}
