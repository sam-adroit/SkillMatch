using System.ComponentModel.DataAnnotations;

namespace SkillMatchBE.Recommendations;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5-mini";

    [Range(3, 30)]
    public int TimeoutSeconds { get; set; } = 15;
}
