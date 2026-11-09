namespace RFactAI.Domain.Exceptions;

public abstract class FactGenerationException : Exception
{
    protected FactGenerationException(string message, Exception? inner = null) : base(message, inner) { }
}
