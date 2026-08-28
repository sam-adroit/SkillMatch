using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public interface ILookupRepository
{
    Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Interest>> GetInterestsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<bool> SkillsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> InterestsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<object?> FindAsync(LookupKind kind, Guid id, CancellationToken cancellationToken);
    Task<bool> NormalizedNameExistsAsync(LookupKind kind, string normalizedName, Guid? exceptId, CancellationToken cancellationToken);
    Task AddAsync(LookupKind kind, string name, string normalizedName, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(LookupKind kind, object entity, CancellationToken cancellationToken);
}

public enum LookupKind
{
    Skill,
    Interest,
    Category
}
