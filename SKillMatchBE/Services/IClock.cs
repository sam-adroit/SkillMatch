namespace SkillMatchBE.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
