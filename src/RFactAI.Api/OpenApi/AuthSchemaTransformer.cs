using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using RFactAI.Application.Auth;
using RFactAI.Domain;

namespace RFactAI.Api.OpenApi;

/// <summary>
/// Gives the register/login request fields a description and example so Swagger UI shows
/// labelled, pre-hinted inputs instead of an anonymous blob. Password is masked in the UI.
/// </summary>
#pragma warning disable CS0618 // OpenApiSchema.Example: Swagger UI reads the singular "example" keyword
public sealed class AuthSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (type != typeof(RegisterRequest) && type != typeof(LoginRequest) || schema.Properties is null)
            return Task.CompletedTask;

        var isRegister = type == typeof(RegisterRequest);

        if (schema.Properties.TryGetValue("email", out var email) && email is OpenApiSchema e)
        {
            e.Description = "Your email address" + (isRegister ? " (must be unique)." : ".");
            e.Format = "email";
            e.Example = JsonValue.Create("user@example.com");
        }

        if (schema.Properties.TryGetValue("password", out var password) && password is OpenApiSchema p)
        {
            p.Description = isRegister
                ? $"{FactLimits.MinPasswordLength}-{FactLimits.MaxPasswordLength} characters."
                : "Your account password.";
            p.Format = "password";
            p.Example = JsonValue.Create("my-password-123");
        }

        return Task.CompletedTask;
    }
}
