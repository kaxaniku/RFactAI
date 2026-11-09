using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace RFactAI.Api.OpenApi;

/// <summary>
/// Adds a JWT bearer scheme so Swagger UI shows an Authorize button. The requirement is document-wide
/// because /api/facts accepts an optional token; the server decides per endpoint what is mandatory.
/// </summary>
public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the accessToken returned by POST /api/auth/login."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}
