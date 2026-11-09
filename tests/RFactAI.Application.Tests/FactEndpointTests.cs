using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RFactAI.Application.Facts;
using static RFactAI.Application.Tests.TestHelpers;

namespace RFactAI.Application.Tests;

public class FactEndpointTests
{
    private static HttpClient CreateClient(IChatClient chat) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
            {
                s.RemoveAll<IChatClient>();
                s.AddSingleton(chat);
            }))
            .CreateClient();

    [Fact]
    public async Task Returns_200_with_facts_and_llm_source_header()
    {
        using var http = CreateClient(ClientReturning(FactsJson("space", "A", "B")));

        var response = await http.GetAsync("/api/facts?category=space&count=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("llm", response.Headers.GetValues("X-Fact-Source").Single());
        var body = await response.Content.ReadFromJsonAsync<FactResponse>();
        Assert.Equal(["A", "B"], body!.Facts);
    }

    [Theory]
    [InlineData("/api/facts?category=space&count=11")]
    [InlineData("/api/facts?category=space&count=0")]
    [InlineData("/api/facts?count=3")]
    [InlineData("/api/facts?category=%20&count=3")]
    public async Task Returns_400_for_invalid_query(string url)
    {
        using var http = CreateClient(ClientReturning(FactsJson("space", "A")));

        var response = await http.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Offline_ollama_known_category_returns_fallback()
    {
        using var http = CreateClient(ClientThrowing(new HttpRequestException("refused")));

        var response = await http.GetAsync("/api/facts?category=space&count=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("fallback", response.Headers.GetValues("X-Fact-Source").Single());
    }

    [Fact]
    public async Task Offline_ollama_unknown_category_returns_503()
    {
        using var http = CreateClient(ClientThrowing(new HttpRequestException("refused")));

        var response = await http.GetAsync("/api/facts?category=quantum-knitting");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
