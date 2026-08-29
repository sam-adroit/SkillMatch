using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SkillMatchBE.Recommendations;

public sealed class OpenAIRecommendationProvider(
    HttpClient httpClient,
    IOptions<OpenAIOptions> options,
    ILogger<OpenAIRecommendationProvider> logger) : IRecommendationProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenAIOptions settings = options.Value;

    public async Task<RecommendationProviderResult> GenerateProjectExplanationsAsync(
        RecommendationExplanationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("OpenAI is not configured.");
        if (request.Projects.Count == 0)
            return new Dictionary<Guid, string>().AsProviderResult(settings.Model);

        var privacySafeInput = new
        {
            student = new
            {
                skills = request.Profile.Skills,
                interests = request.Profile.Interests,
                preferredTechnologies = request.Profile.PreferredTechnologies
            },
            projects = request.Projects.Select((project, index) => new
            {
                index,
                project.Title,
                project.Category,
                project.Difficulty,
                project.RequiredSkills,
                project.Score,
                project.MatchedSkills,
                project.MissingSkills
            })
        };

        var payload = new
        {
            model = settings.Model,
            store = false,
            max_output_tokens = 2000,
            reasoning = new { effort = "minimal" },
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "Write one concise, encouraging, evidence-based project-fit explanation per supplied index. " +
                              "Mention concrete matched strengths and one realistic growth area when skills are missing. " +
                              "Do not make admission, approval, or assignment decisions. Return exactly one item per project."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(privacySafeInput, JsonOptions)
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "skillmatch_project_explanations",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            results = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        index = new { type = "integer" },
                                        explanation = new { type = "string" }
                                    },
                                    required = new[] { "index", "explanation" },
                                    additionalProperties = false
                                }
                            }
                        },
                        required = new[] { "results" },
                        additionalProperties = false
                    }
                }
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenAI Responses API returned HTTP {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"OpenAI Responses API returned HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var outputText = ExtractOutputText(document.RootElement);
        var parsed = JsonSerializer.Deserialize<StructuredExplanationResponse>(outputText, JsonOptions)
            ?? throw new JsonException("OpenAI returned no structured explanation payload.");

        var explanations = new Dictionary<Guid, string>();
        foreach (var item in parsed.Results)
        {
            if (item.Index < 0 || item.Index >= request.Projects.Count || string.IsNullOrWhiteSpace(item.Explanation))
                throw new JsonException("OpenAI returned an invalid recommendation index or explanation.");
            if (!explanations.TryAdd(request.Projects[item.Index].ProjectId, item.Explanation.Trim()))
                throw new JsonException("OpenAI returned a duplicate recommendation index.");
        }

        if (explanations.Count != request.Projects.Count)
            throw new JsonException("OpenAI did not return an explanation for every recommendation.");

        return new(explanations, "OpenAI", settings.Model);
    }

    private static string ExtractOutputText(JsonElement response)
    {
        if (!response.TryGetProperty("output", out var output))
            throw new JsonException("OpenAI response did not contain output.");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content))
                continue;
            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text) && !string.IsNullOrWhiteSpace(text.GetString()))
                    return text.GetString()!;
            }
        }

        throw new JsonException("OpenAI response contained no output text.");
    }

    private sealed record StructuredExplanationResponse(IReadOnlyList<StructuredExplanation> Results);
    private sealed record StructuredExplanation(int Index, string Explanation);
}

internal static class RecommendationProviderResultExtensions
{
    public static RecommendationProviderResult AsProviderResult(
        this IReadOnlyDictionary<Guid, string> explanations,
        string model) => new(explanations, "OpenAI", model);
}
