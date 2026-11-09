using Microsoft.EntityFrameworkCore;
using RFactAI.Application.Abstractions;
using RFactAI.Domain.Entities;
using RFactAI.Domain.Exceptions;

namespace RFactAI.Application.Auth;

public sealed class AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalized = AuthValidator.NormalizeEmail(email);

        if (await db.Users.AnyAsync(u => u.Email == normalized, cancellationToken))
            throw new EmailAlreadyRegisteredException();

        var user = new User { Email = normalized, PasswordHash = hasher.Hash(password) };
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two concurrent registrations can both pass the check above; the unique index decides.
            if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == normalized, cancellationToken))
                throw new EmailAlreadyRegisteredException(ex);
            throw;
        }

        return new UserResponse(user.Id, user.Email);
    }

    public async Task<TokenResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalized = AuthValidator.NormalizeEmail(email);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);

        if (user is null || !hasher.Verify(user.PasswordHash, password))
            throw new InvalidCredentialsException();

        return tokens.Create(user);
    }
}
