using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RFactAI.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = "RFactAI";
    public string Audience { get; set; } = "RFactAI.Clients";

    /// <summary>Secret, HS256. Keep out of source control (user-secrets / environment variable).</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public static bool HasValidSigningKey(JwtOptions o) =>
        Encoding.UTF8.GetByteCount(o.SigningKey ?? string.Empty) >= MinSigningKeyBytes;
}
