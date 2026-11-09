using System.Text.Json;

namespace RFactAI.Api.Endpoints;

/// <summary>
/// Email/password pair bound from either form fields (what Swagger UI renders as separate inputs)
/// or a JSON body (what API clients typically send). Missing or malformed bodies bind as null so the
/// endpoint can answer with its normal 400 validation response.
/// </summary>
public sealed record Credentials(string? Email, string? Password)
{
    public static async ValueTask<Credentials?> BindAsync(HttpContext context)
    {
        var request = context.Request;
        try
        {
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(context.RequestAborted);
                return new Credentials(form["email"].ToString(), form["password"].ToString());
            }

            if (request.HasJsonContentType())
            {
                using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: context.RequestAborted);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                    ? new Credentials(ReadString(doc.RootElement, "email"), ReadString(doc.RootElement, "password"))
                    : null;
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or BadHttpRequestException)
        {
            // Empty or malformed body: treated as "no credentials supplied".
        }

        return null;
    }

    private static string? ReadString(JsonElement obj, string name)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }

        return null;
    }
}
