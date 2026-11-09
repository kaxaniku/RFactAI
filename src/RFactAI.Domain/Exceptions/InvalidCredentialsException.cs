namespace RFactAI.Domain.Exceptions;

/// <summary>Deliberately one message for unknown email and wrong password, so accounts can't be enumerated.</summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password.") { }
}
