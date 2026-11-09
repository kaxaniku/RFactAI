using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using RFactAI.Application.Facts;

namespace RFactAI.Infrastructure;

public static class DependencyInjection
{
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
        return services;
    }
}
