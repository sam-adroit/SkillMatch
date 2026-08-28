using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SkillMatchBE.Data;

public sealed class SkillMatchDbContextFactory : IDesignTimeDbContextFactory<SkillMatchDbContext>
{
    public SkillMatchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SkillMatchDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Port=5432;Database=skillmatch_design;Username=design;Password=design")
            .Options;

        return new SkillMatchDbContext(options);
    }
}
