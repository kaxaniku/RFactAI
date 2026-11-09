using RFactAI.Application.Facts;
using RFactAI.Domain;
using RFactAI.Domain.Exceptions;

namespace RFactAI.Api.Endpoints;

public static class FactEndpoints
{
    public const string SourceHeader = "X-Fact-Source";

    public static IEndpointRouteBuilder MapFactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/facts")
            .WithTags("Facts")
            .MapGet("/", GetFacts)
            .WithName("GetFacts")
            .WithSummary("Get random facts")
            .WithDescription(
                $"Generates facts for a category using the local LLM. `count` is {FactLimits.MinCount}-{FactLimits.MaxCount} (default {FactLimits.DefaultCount}). " +
                $"The `{SourceHeader}` response header is `llm` or `fallback` (curated facts used when the LLM is offline or returns invalid JSON). " +
                "Returns 503 when the LLM is unavailable and the category has no fallback facts.")
            .Produces<FactResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> GetFacts(
        string? category,
        int? count,
        IFactService facts,
        HttpResponse response,
        CancellationToken ct)
    {
        var errors = Validate(category, count);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var result = await facts.GetFactsAsync(category!.Trim(), count ?? FactLimits.DefaultCount, ct);
            response.Headers[SourceHeader] = result.Source.ToString().ToLowerInvariant();
            return Results.Ok(result.Response);
        }
        catch (LlmUnavailableException ex)
        {
            return Results.Problem(
                title: "Fact generation unavailable",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static Dictionary<string, string[]> Validate(string? category, int? count)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(category))
            errors["category"] = ["category is required."];
        else if (category.Trim().Length > FactLimits.MaxCategoryLength)
            errors["category"] = [$"category must be at most {FactLimits.MaxCategoryLength} characters."];

        if (count is { } c && (c < FactLimits.MinCount || c > FactLimits.MaxCount))
            errors["count"] = [$"count must be between {FactLimits.MinCount} and {FactLimits.MaxCount}."];

        return errors;
    }
}
