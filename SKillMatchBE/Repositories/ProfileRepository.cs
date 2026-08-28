using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Data;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public sealed class ProfileRepository(SkillMatchDbContext database) : IProfileRepository
{
    public Task<StudentProfile?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        database.StudentProfiles
            .Include(item => item.User)
            .Include(item => item.Skills).ThenInclude(item => item.Skill)
            .Include(item => item.Interests).ThenInclude(item => item.Interest)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

    public async Task SaveAsync(StudentProfile profile, CancellationToken cancellationToken)
    {
        if (database.Entry(profile).State == EntityState.Detached)
        {
            database.StudentProfiles.Add(profile);
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
