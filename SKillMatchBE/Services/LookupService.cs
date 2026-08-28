using SkillMatchBE.DTOs.Catalog;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public sealed class LookupService(ILookupRepository repository) : ILookupService
{
    public async Task<IReadOnlyList<LookupResponse>> GetAsync(LookupKind kind, CancellationToken cancellationToken) => kind switch
    {
        LookupKind.Skill => (await repository.GetSkillsAsync(cancellationToken)).Select(item => new LookupResponse(item.Id, item.Name)).ToArray(),
        LookupKind.Interest => (await repository.GetInterestsAsync(cancellationToken)).Select(item => new LookupResponse(item.Id, item.Name)).ToArray(),
        LookupKind.Category => (await repository.GetCategoriesAsync(cancellationToken)).Select(item => new LookupResponse(item.Id, item.Name)).ToArray(),
        _ => []
    };

    public async Task<LookupServiceResult> CreateAsync(LookupKind kind, SaveLookupRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalized = Normalize(name);
        if (await repository.NormalizedNameExistsAsync(kind, normalized, null, cancellationToken))
            return new(null, LookupServiceFailure.DuplicateName);

        await repository.AddAsync(kind, name, normalized, cancellationToken);
        var created = (await GetAsync(kind, cancellationToken)).Single(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        return new(created, LookupServiceFailure.None);
    }

    public async Task<LookupServiceResult> UpdateAsync(LookupKind kind, Guid id, SaveLookupRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.FindAsync(kind, id, cancellationToken);
        if (entity is null) return new(null, LookupServiceFailure.NotFound);

        var name = request.Name.Trim();
        var normalized = Normalize(name);
        if (await repository.NormalizedNameExistsAsync(kind, normalized, id, cancellationToken))
            return new(null, LookupServiceFailure.DuplicateName);

        switch (entity)
        {
            case Skill skill: skill.Name = name; skill.NormalizedName = normalized; break;
            case Interest interest: interest.Name = name; interest.NormalizedName = normalized; break;
            case Category category: category.Name = name; category.NormalizedName = normalized; break;
        }
        await repository.SaveAsync(cancellationToken);
        return new(new LookupResponse(id, name), LookupServiceFailure.None);
    }

    public async Task<LookupServiceFailure> DeleteAsync(LookupKind kind, Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.FindAsync(kind, id, cancellationToken);
        if (entity is null) return LookupServiceFailure.NotFound;
        return await repository.DeleteAsync(kind, entity, cancellationToken)
            ? LookupServiceFailure.None
            : LookupServiceFailure.InUse;
    }

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
