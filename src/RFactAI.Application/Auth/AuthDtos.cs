namespace RFactAI.Application.Auth;

public sealed record RegisterRequest(string? Email, string? Password);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record UserResponse(Guid Id, string Email);

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);
