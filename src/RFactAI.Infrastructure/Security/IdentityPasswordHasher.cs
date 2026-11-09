using Microsoft.AspNetCore.Identity;
using RFactAI.Application.Abstractions;
using RFactAI.Domain.Entities;

namespace RFactAI.Infrastructure.Security;

/// <summary>PBKDF2 hashing with per-password salt, via ASP.NET Core Identity's PasswordHasher.</summary>
public sealed class IdentityPasswordHasher : Application.Abstractions.IPasswordHasher
{
    private static readonly User Placeholder = new();
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(Placeholder, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(Placeholder, hash, password)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
