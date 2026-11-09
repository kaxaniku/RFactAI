using System.Net.Mail;
using RFactAI.Domain;

namespace RFactAI.Application.Auth;

public static class AuthValidator
{
    public static Dictionary<string, string[]> ValidateRegistration(string? email, string? password)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(email))
            errors["email"] = ["email is required."];
        else if (!IsValidEmail(email.Trim()))
            errors["email"] = ["email is not a valid address."];

        if (string.IsNullOrEmpty(password))
            errors["password"] = ["password is required."];
        else if (password.Length < FactLimits.MinPasswordLength || password.Length > FactLimits.MaxPasswordLength)
            errors["password"] =
                [$"password must be {FactLimits.MinPasswordLength}-{FactLimits.MaxPasswordLength} characters."];

        return errors;
    }

    public static Dictionary<string, string[]> ValidateLogin(string? email, string? password)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(email)) errors["email"] = ["email is required."];
        if (string.IsNullOrEmpty(password)) errors["password"] = ["password is required."];
        return errors;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email) =>
        email.Length <= FactLimits.MaxEmailLength
        && MailAddress.TryCreate(email, out var parsed)
        && parsed.Address == email
        && email.Contains('@');
}
