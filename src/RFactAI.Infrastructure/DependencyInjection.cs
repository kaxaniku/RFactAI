using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using RFactAI.Application.Abstractions;
using RFactAI.Application.Auth;
using RFactAI.Application.Facts;
using RFactAI.Application.History;
using RFactAI.Infrastructure.Persistence;
using RFactAI.Infrastructure.Security;

namespace RFactAI.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        services.AddChatClient(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
                var http = new HttpClient
                {
                    BaseAddress = new Uri(options.Endpoint),
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
                };
                return new OllamaApiClient(http, options.Model);
            })
            .UseLogging();

        services.AddSingleton<IFallbackFactProvider, StaticFallbackFactProvider>();

        // Configuration is read lazily (inside factories / options) so hosts and tests can override it.
        services.AddDbContext<AppDbContext>((sp, db) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured. " +
                    "Set it with: dotnet user-secrets set \"ConnectionStrings:Default\" \"Host=...;Database=rfactai;Username=...;Password=...\"");
            db.UseNpgsql(connectionString);
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(JwtOptions.HasValidSigningKey,
                $"Jwt:SigningKey must be at least {JwtOptions.MinSigningKeyBytes} bytes. " +
                "Set it with: dotnet user-secrets set \"Jwt:SigningKey\" \"<random 64+ char secret>\"")
            .ValidateOnStart();

        services.AddSingleton<Application.Abstractions.IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHistoryService, HistoryService>();

        return services;
    }
}
