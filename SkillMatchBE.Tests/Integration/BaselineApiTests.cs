using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SkillMatchBE.Tests.Integration;

public sealed class BaselineApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public BaselineApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=127.0.0.1;Port=1;Database=skillmatch_test;Username=test;Password=test");
                builder.UseSetting("Jwt:Issuer", "SkillMatchBE.Tests");
                builder.UseSetting("Jwt:Audience", "SkillMatchFE.Tests");
                builder.UseSetting("Jwt:Key", "test-only-signing-key-at-least-32-bytes-long");
            })
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task SwaggerJson_IsAvailable()
    {
        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<SwaggerDocument>();

        Assert.NotNull(document);
        Assert.Equal("3.0.4", document.Openapi);
        Assert.Contains("/health/database", document.Paths.Keys);
        Assert.Contains("/api/profile", document.Paths.Keys);
        Assert.Contains("/api/projects", document.Paths.Keys);
        Assert.Contains("/api/admin/projects", document.Paths.Keys);
        var bearer = Assert.Contains("Bearer", document.Components.SecuritySchemes);
        Assert.Equal("http", bearer.Type);
        Assert.Equal("bearer", bearer.Scheme);
        Assert.Equal("JWT", bearer.BearerFormat);
        var globalSecurity = Assert.Single(document.Security);
        Assert.True(globalSecurity.TryGetProperty("Bearer", out var scopes));
        Assert.Equal(JsonValueKind.Array, scopes.ValueKind);
        Assert.Empty(scopes.EnumerateArray());
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails()
    {
        using var response = await client.GetAsync("/not-a-real-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal(404, problem?.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem?.TraceId));
    }

    private sealed record SwaggerDocument(
        string Openapi,
        Dictionary<string, object> Paths,
        SwaggerComponents Components,
        JsonElement[] Security);

    private sealed record SwaggerComponents(
        Dictionary<string, SwaggerSecurityScheme> SecuritySchemes);

    private sealed record SwaggerSecurityScheme(
        string Type,
        string Scheme,
        string BearerFormat);

    private sealed record ProblemResponse(int Status, string TraceId);
}
