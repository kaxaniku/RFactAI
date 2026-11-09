namespace RFactAI.Domain.Exceptions;

public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException(Exception? inner = null)
        : base("An account with this email already exists.", inner) { }
}
