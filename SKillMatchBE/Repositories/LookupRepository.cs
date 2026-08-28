using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Data;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public sealed class LookupRepository(SkillMatchDbContext database) : ILookupRepository
{
    public async Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken cancellationToken) =>
        await database.Skills.OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Interest>> GetInterestsAsync(CancellationToken cancellationToken) =>
        await database.Interests.OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await database.Categories.OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public async Task<bool> SkillsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count > 0 && await database.Skills.CountAsync(item => ids.Contains(item.Id), cancellationToken) == ids.Count;

    public async Task<bool> InterestsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count > 0 && await database.Interests.CountAsync(item => ids.Contains(item.Id), cancellationToken) == ids.Count;

    public Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken) =>
        database.Categories.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<object?> FindAsync(LookupKind kind, Guid id, CancellationToken cancellationToken) => kind switch
    {
        LookupKind.Skill => await database.Skills.FindAsync([id], cancellationToken),
        LookupKind.Interest => await database.Interests.FindAsync([id], cancellationToken),
        LookupKind.Category => await database.Categories.FindAsync([id], cancellationToken),
        _ => null
    };

    public Task<bool> NormalizedNameExistsAsync(
        LookupKind kind,
        string normalizedName,
        Guid? exceptId,
        CancellationToken cancellationToken) => kind switch
    {
        LookupKind.Skill => database.Skills.AnyAsync(item => item.NormalizedName == normalizedName && item.Id != exceptId, cancellationToken),
        LookupKind.Interest => database.Interests.AnyAsync(item => item.NormalizedName == normalizedName && item.Id != exceptId, cancellationToken),
        LookupKind.Category => database.Categories.AnyAsync(item => item.NormalizedName == normalizedName && item.Id != exceptId, cancellationToken),
        _ => Task.FromResult(false)
    };

    public async Task AddAsync(LookupKind kind, string name, string normalizedName, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case LookupKind.Skill:
                database.Skills.Add(new Skill { Name = name, NormalizedName = normalizedName });
                break;
            case LookupKind.Interest:
                database.Interests.Add(new Interest { Name = name, NormalizedName = normalizedName });
                break;
            case LookupKind.Category:
                database.Categories.Add(new Category { Name = name, NormalizedName = normalizedName });
                break;
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);

    public async Task<bool> DeleteAsync(LookupKind kind, object entity, CancellationToken cancellationToken)
    {
        database.Remove(entity);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            database.Entry(entity).State = EntityState.Unchanged;
            return false;
        }
    }
}
