namespace SkillMatchBE.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrations { get; init; }
}
