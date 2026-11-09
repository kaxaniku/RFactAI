using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using RFactAI.Application.Auth;
using RFactAI.Domain.Exceptions;
using RFactAI.Infrastructure.Security;

namespace RFactAI.Application.Tests;

public class AuthServiceTests
{
    private static (AuthService Service, Infrastructure.Persistence.AppDbContext Db) Create()
    {
        var db = TestApi.CreateDb();
        var jwt = Options.Create(new JwtOptions { SigningKey = TestApi.SigningKey });
        return (new AuthService(db, new IdentityPasswordHasher(), new JwtTokenService(jwt)), db);
    }

    [Fact]
    public async Task Register_stores_normalized_email_and_a_hash_not_the_password()
    {
        var (auth, db) = Create();

        var user = await auth.RegisterAsync("  Bob@Example.COM ", "correct horse battery");

        Assert.Equal("bob@example.com", user.Email);
        var stored = await db.Users.SingleAsync();
        Assert.Equal("bob@example.com", stored.Email);
        Assert.NotEqual("correct horse battery", stored.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(stored.PasswordHash));
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_ignoring_case()
    {
        var (auth, _) = Create();
        await auth.RegisterAsync("bob@example.com", "password-1");

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => auth.RegisterAsync("BOB@example.com", "password-2"));
    }

    [Fact]
    public async Task Login_returns_jwt_for_correct_credentials_regardless_of_email_case()
    {
        var (auth, _) = Create();
        var user = await auth.RegisterAsync("bob@example.com", "password-1");

        var token = await auth.LoginAsync("Bob@Example.com", "password-1");

        Assert.True(token.ExpiresAt > DateTime.UtcNow);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
    }

    [Fact]
    public async Task Login_fails_for_wrong_password()
    {
        var (auth, _) = Create();
        await auth.RegisterAsync("bob@example.com", "password-1");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => auth.LoginAsync("bob@example.com", "nope-nope"));
    }

    [Fact]
    public async Task Login_fails_for_unknown_email()
    {
        var (auth, _) = Create();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => auth.LoginAsync("ghost@example.com", "password-1"));
    }

    [Theory]
    [InlineData(null, "password-1")]
    [InlineData("", "password-1")]
    [InlineData("not-an-email", "password-1")]
    [InlineData("Bob <bob@example.com>", "password-1")]
    [InlineData("bob@example.com", null)]
    [InlineData("bob@example.com", "short")]
    public void Validator_rejects_bad_registration(string? email, string? password)
    {
        Assert.NotEmpty(AuthValidator.ValidateRegistration(email, password));
    }

    [Fact]
    public void Validator_accepts_good_registration()
    {
        Assert.Empty(AuthValidator.ValidateRegistration("bob@example.com", "password-1"));
    }
}
