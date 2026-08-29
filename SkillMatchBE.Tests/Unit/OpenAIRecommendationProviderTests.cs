using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkillMatchBE.Recommendations;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class OpenAIRecommendationProviderTests
{
    [Fact]
    public async Task Request_UsesResponsesStructuredOutputAndExcludesPrivateFieldsAndIdentifiers()
    {
        const string responseJson = """
            {"output":[{"content":[{"type":"output_text","text":"{\"results\":[{\"index\":0,\"explanation\":\"Strong fit.\"}]}"}]}]}
            """;
        var handler = new RecordingHandler(responseJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var provider = new OpenAIRecommendationProvider(
            client,
            Options.Create(new OpenAIOptions { ApiKey = "test-key", Model = "gpt-5-mini" }),
            NullLogger<OpenAIRecommendationProvider>.Instance);
        var projectId = Guid.Parse("fe359457-27a8-4c34-a50e-bcd389966538");

        var result = await provider.GenerateProjectExplanationsAsync(
            new(
                new(["C#"], ["Education"], ["PostgreSQL"]),
                [new(projectId, "Campus Hub", "Education", "Intermediate", ["C#"], 92m, ["C#"], [])]),
            default);

        Assert.Equal("Strong fit.", result.Explanations[projectId]);
        Assert.Contains("/v1/responses", handler.RequestUri);
        Assert.Contains("\"text\"", handler.RequestBody);
        Assert.Contains("\"json_schema\"", handler.RequestBody);
        Assert.Contains("Campus Hub", handler.RequestBody);
        Assert.DoesNotContain(projectId.ToString(), handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private@example.edu", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("application note", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private goals", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingHandler(string responseJson) : HttpMessageHandler
    {
        public string RequestUri { get; private set; } = string.Empty;
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            RequestUri = request.RequestUri!.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
