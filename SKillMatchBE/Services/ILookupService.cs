using SkillMatchBE.DTOs.Catalog;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public interface ILookupService
{
    Task<IReadOnlyList<LookupResponse>> GetAsync(LookupKind kind, CancellationToken cancellationToken);
    Task<LookupServiceResult> CreateAsync(LookupKind kind, SaveLookupRequest request, CancellationToken cancellationToken);
    Task<LookupServiceResult> UpdateAsync(LookupKind kind, Guid id, SaveLookupRequest request, CancellationToken cancellationToken);
    Task<LookupServiceFailure> DeleteAsync(LookupKind kind, Guid id, CancellationToken cancellationToken);
}

public enum LookupServiceFailure { None, NotFound, DuplicateName, InUse }
public sealed record LookupServiceResult(LookupResponse? Lookup, LookupServiceFailure Failure);
