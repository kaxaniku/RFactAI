using RFactAI.Application.Auth;
using RFactAI.Domain.Exceptions;

namespace RFactAI.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Create an account")
            .WithDescription("Fill in the Email and Password fields. Email must be valid and unique (case-insensitive); password must be 8-128 characters. A JSON body `{\"email\":\"...\",\"password\":\"...\"}` is also accepted.")
            .Accepts<RegisterRequest>("application/x-www-form-urlencoded", "application/json")
            .DisableAntiforgery()
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Get a JWT access token")
            .WithDescription("Fill in Email and Password, then copy the returned `accessToken` into the Authorize button in Swagger (or send it as `Authorization: Bearer <token>`). A JSON body is also accepted.")
            .Accepts<LoginRequest>("application/x-www-form-urlencoded", "application/json")
            .DisableAntiforgery()
            .Produces<TokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> Register(Credentials? request, IAuthService auth, CancellationToken ct)
    {
        var errors = AuthValidator.ValidateRegistration(request?.Email, request?.Password);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var user = await auth.RegisterAsync(request!.Email!, request.Password!, ct);
            return Results.Json(user, statusCode: StatusCodes.Status201Created);
        }
        catch (EmailAlreadyRegisteredException ex)
        {
            return Results.Problem(
                title: "Email already registered",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> Login(Credentials? request, IAuthService auth, CancellationToken ct)
    {
        var errors = AuthValidator.ValidateLogin(request?.Email, request?.Password);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            return Results.Ok(await auth.LoginAsync(request!.Email!, request.Password!, ct));
        }
        catch (InvalidCredentialsException ex)
        {
            return Results.Problem(
                title: "Invalid credentials",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }
}
