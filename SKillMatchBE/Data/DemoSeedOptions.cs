namespace SkillMatchBE.Data;

public sealed class DemoSeedOptions
{
    public const string SectionName = "DemoSeed";

    public bool Enabled { get; init; }

    public string AdminEmail { get; init; } = string.Empty;

    public string AdminPassword { get; init; } = string.Empty;
}
