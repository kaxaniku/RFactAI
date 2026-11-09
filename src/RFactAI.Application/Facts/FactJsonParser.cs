using System.Text.Json;
using RFactAI.Domain.Exceptions;

namespace RFactAI.Application.Facts;

public static class FactJsonParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Parses the model output into a <see cref="FactResponse"/>, tolerating markdown code fences.
    /// Extra facts are trimmed to <paramref name="count"/>; the requested category wins when the model omits one.
    /// </summary>
    /// <exception cref="InvalidLlmResponseException">The text is not the expected JSON shape.</exception>
    public static FactResponse Parse(string? text, string requestedCategory, int count)
    {
        var json = StripFences(text);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidLlmResponseException("The model returned an empty response.");

        FactResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<FactResponse>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidLlmResponseException("The model returned malformed JSON.", ex);
        }

        var facts = parsed?.Facts?
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Take(count)
            .ToList();

        if (facts is not { Count: > 0 })
            throw new InvalidLlmResponseException("The model returned no usable facts.");

        var category = string.IsNullOrWhiteSpace(parsed!.Category) ? requestedCategory : parsed.Category.Trim();
        return new FactResponse(category, facts);
    }

    private static string StripFences(string? text)
    {
        var s = text?.Trim() ?? string.Empty;
        if (!s.StartsWith("```", StringComparison.Ordinal)) return s;

        var firstNewline = s.IndexOf('\n');
        if (firstNewline < 0) return string.Empty;

        s = s[(firstNewline + 1)..];
        var closing = s.LastIndexOf("```", StringComparison.Ordinal);
        return (closing >= 0 ? s[..closing] : s).Trim();
    }
}
