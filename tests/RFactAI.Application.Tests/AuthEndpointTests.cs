using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.AI;
using RFactAI.Application.Auth;
using RFactAI.Application.History;
using static RFactAI.Application.Tests.TestHelpers;

namespace RFactAI.Application.Tests;

public class AuthEndpointTests
{
    private static HttpClient CreateClient(IChatClient? chat = null) =>
        TestApi.CreateFactory(chat ?? ClientReturning(FactsJson("space", "A", "B", "C"))).CreateClient();

    private static async Task<string> RegisterAndLogin(HttpClient http, string email = "bob@example.com")
    {
        var register = await http.PostAsJsonAsync("/api/auth/register", new { email, password = "password-1" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await http.PostAsJsonAsync("/api/auth/login", new { email, password = "password-1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
    }

    private static HttpRequestMessage Get(string url, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Register_returns_201_then_409_for_duplicate()
    {
        using var http = CreateClient();

        var first = await http.PostAsJsonAsync("/api/auth/register", new { email = "bob@example.com", password = "password-1" });
        var second = await http.PostAsJsonAsync("/api/auth/register", new { email = "BOB@example.com", password = "password-2" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var body = await first.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("bob@example.com", body!.Email);
        Assert.DoesNotContain("password", await first.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("", "password-1")]
    [InlineData("not-an-email", "password-1")]
    [InlineData("bob@example.com", "short")]
    [InlineData("bob@example.com", "")]
    public async Task Register_returns_400_for_invalid_input(string email, string password)
    {
        using var http = CreateClient();

        var response = await http.PostAsJsonAsync("/api/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_and_login_return_400_when_body_is_missing()
    {
        using var http = CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsync("/api/auth/register", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsync("/api/auth/login", null)).StatusCode);
    }

    private static FormUrlEncodedContent Form(string? email, string? password) =>
        new(new Dictionary<string, string>
        {
            ["email"] = email ?? "",
            ["password"] = password ?? ""
        });

    [Fact]
    public async Task Register_and_login_accept_form_fields()
    {
        using var http = CreateClient();

        var register = await http.PostAsync("/api/auth/register", Form("form@example.com", "password-1"));
        var login = await http.PostAsync("/api/auth/login", Form("form@example.com", "password-1"));
        var wrong = await http.PostAsync("/api/auth/login", Form("form@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.False(string.IsNullOrEmpty((await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Theory]
    [InlineData("", "password-1")]
    [InlineData("bob@example.com", "short")]
    [InlineData("bob@example.com", "")]
    public async Task Form_register_returns_400_for_invalid_input(string email, string password)
    {
        using var http = CreateClient();

        var response = await http.PostAsync("/api/auth/register", Form(email, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_json_returns_400()
    {
        using var http = CreateClient();
        using var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsync("/api/auth/login", content)).StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_advertises_form_fields_for_register_and_login()
    {
        using var http = CreateClient();

        var doc = System.Text.Json.Nodes.JsonNode.Parse(await http.GetStringAsync("/openapi/v1.json"))!;

        foreach (var path in new[] { "/api/auth/register", "/api/auth/login" })
        {
            var content = doc["paths"]![path]!["post"]!["requestBody"]!["content"]!.AsObject();
            Assert.Contains("application/x-www-form-urlencoded", content.Select(kv => kv.Key));
            Assert.Contains("application/json", content.Select(kv => kv.Key));
        }
    }

    [Fact]
    public async Task Login_returns_401_for_wrong_password_and_unknown_user()
    {
        using var http = CreateClient();
        await RegisterAndLogin(http);

        var wrong = await http.PostAsJsonAsync("/api/auth/login", new { email = "bob@example.com", password = "wrong-password" });
        var unknown = await http.PostAsJsonAsync("/api/auth/login", new { email = "ghost@example.com", password = "password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    [Fact]
    public async Task History_requires_a_valid_token()
    {
        using var http = CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.SendAsync(Get("/api/history"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.SendAsync(Get("/api/history", "garbage.token.value"))).StatusCode);
    }

    [Fact]
    public async Task Authenticated_fact_requests_are_saved_to_history()
    {
        using var http = CreateClient();
        var token = await RegisterAndLogin(http);

        var facts = await http.SendAsync(Get("/api/facts?category=space&count=3", token));
        Assert.Equal(HttpStatusCode.OK, facts.StatusCode);

        var history = await http.SendAsync(Get("/api/history", token));
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var items = await history.Content.ReadFromJsonAsync<List<HistoryItem>>();
        Assert.NotNull(items);
        Assert.Equal(["A", "B", "C"], items.Select(i => i.Fact));
        Assert.All(items, i =>
        {
            Assert.Equal("space", i.Category);
            Assert.Equal("llm", i.Source);
        });
    }

    [Fact]
    public async Task Anonymous_fact_requests_still_work_and_are_not_saved()
    {
        using var http = CreateClient();
        var token = await RegisterAndLogin(http);

        var anonymous = await http.SendAsync(Get("/api/facts?category=space&count=3"));
        Assert.Equal(HttpStatusCode.OK, anonymous.StatusCode);

        var items = await (await http.SendAsync(Get("/api/history", token))).Content.ReadFromJsonAsync<List<HistoryItem>>();
        Assert.Empty(items!);
    }

    [Fact]
    public async Task Each_user_only_sees_their_own_history()
    {
        using var http = CreateClient();
        var bob = await RegisterAndLogin(http, "bob@example.com");
        var alice = await RegisterAndLogin(http, "alice@example.com");

        await http.SendAsync(Get("/api/facts?category=space&count=3", bob));

        var aliceItems = await (await http.SendAsync(Get("/api/history", alice))).Content.ReadFromJsonAsync<List<HistoryItem>>();
        var bobItems = await (await http.SendAsync(Get("/api/history", bob))).Content.ReadFromJsonAsync<List<HistoryItem>>();
        Assert.Empty(aliceItems!);
        Assert.Equal(3, bobItems!.Count);
    }
}
