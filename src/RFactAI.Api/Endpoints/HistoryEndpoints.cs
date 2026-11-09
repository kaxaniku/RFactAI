using System.Security.Claims;
using RFactAI.Application.History;
using RFactAI.Domain;

namespace RFactAI.Api.Endpoints;

public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/history")
            .WithTags("History")
            .RequireAuthorization()
            .MapGet("/", GetHistory)
            .WithName("GetHistory")
            .WithSummary("Get my saved facts")
            .WithDescription($"The signed-in user's saved facts, newest first (at most {FactLimits.MaxHistoryPerUser}).")
            .Produces<IReadOnlyList<HistoryItem>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetHistory(ClaimsPrincipal principal, IHistoryService history, CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
            return Results.Unauthorized();

        return Results.Ok(await history.ListAsync(userId, ct));
    }
}
