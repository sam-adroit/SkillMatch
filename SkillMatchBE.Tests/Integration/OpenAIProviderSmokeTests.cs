using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using SkillMatchBE.Recommendations;
using Xunit;

namespace SkillMatchBE.Tests.Integration;

public sealed class OpenAIProviderSmokeTests
{
    [Fact]
    [Trait("Category", "OpenAISmoke")]
    public async Task LiveResponsesApi_ReturnsNonEmptyStructuredExplanation()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_OPENAI_SMOKE_TEST"), "1", StringComparison.Ordinal))
            return;

        var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
        var key = configuration["OPENAI_API_KEY"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Configure OPENAI_API_KEY in SkillMatchBE user-secrets before running the live smoke test.");
        var model = configuration["OPENAI_MODEL"] ?? "gpt-5-mini";
        var provider = new OpenAIRecommendationProvider(
            new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") },
            Options.Create(new OpenAIOptions { ApiKey = key!, Model = model, TimeoutSeconds = 30 }),
            NullLogger<OpenAIRecommendationProvider>.Instance);
        var projectId = Guid.NewGuid();

        var result = await provider.GenerateProjectExplanationsAsync(
            new(
                new(["C#", "PostgreSQL"], ["Education"], ["React"]),
                [new(projectId, "Campus Collaboration Hub", "Education", "Intermediate", ["C#", "PostgreSQL"], 95m, ["C#", "PostgreSQL"], [])]),
            default);

        Assert.Equal("OpenAI", result.Provider);
        Assert.False(string.IsNullOrWhiteSpace(result.Explanations[projectId]));
    }
}
