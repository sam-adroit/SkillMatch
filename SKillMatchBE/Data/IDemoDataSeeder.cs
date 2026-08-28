namespace SkillMatchBE.Data;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
