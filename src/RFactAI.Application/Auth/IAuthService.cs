namespace RFactAI.Application.Auth;

public interface IAuthService
{
    /// <exception cref="RFactAI.Domain.Exceptions.EmailAlreadyRegisteredException"/>
    Task<UserResponse> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <exception cref="RFactAI.Domain.Exceptions.InvalidCredentialsException"/>
    Task<TokenResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
