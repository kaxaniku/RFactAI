using RFactAI.Application.Auth;
using RFactAI.Domain.Entities;

namespace RFactAI.Application.Abstractions;

public interface ITokenService
{
    TokenResponse Create(User user);
}
