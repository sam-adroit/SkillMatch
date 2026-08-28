using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public interface IProfileRepository
{
    Task<StudentProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveAsync(StudentProfile profile, CancellationToken cancellationToken);
}
