using SkillMatchBE.DTOs.Catalog;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class LookupServiceTests
{
    [Fact]
    public async Task Create_TrimsAndNormalizesName()
    {
        var repository = new MemoryLookupRepository();
        var result = await new LookupService(repository).CreateAsync(
            LookupKind.Skill,
            new SaveLookupRequest("  TypeScript  "),
            CancellationToken.None);

        Assert.Equal(LookupServiceFailure.None, result.Failure);
        Assert.Equal("TypeScript", result.Lookup?.Name);
        Assert.Equal("TYPESCRIPT", repository.Skills.Single().NormalizedName);
    }

    [Fact]
    public async Task Create_RejectsCaseInsensitiveDuplicate()
    {
        var repository = new MemoryLookupRepository();
        repository.Skills.Add(new Skill { Name = "React", NormalizedName = "REACT" });

        var result = await new LookupService(repository).CreateAsync(
            LookupKind.Skill,
            new SaveLookupRequest(" react "),
            CancellationToken.None);

        Assert.Equal(LookupServiceFailure.DuplicateName, result.Failure);
        Assert.Single(repository.Skills);
    }

    [Fact]
    public async Task Update_ReturnsNotFoundAndDoesNotSave()
    {
        var repository = new MemoryLookupRepository();

        var result = await new LookupService(repository).UpdateAsync(
            LookupKind.Category,
            Guid.NewGuid(),
            new SaveLookupRequest("Education"),
            CancellationToken.None);

        Assert.Equal(LookupServiceFailure.NotFound, result.Failure);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task Delete_ReportsInUseWithoutRemovingLookup()
    {
        var repository = new MemoryLookupRepository { DeleteAllowed = false };
        var skill = new Skill { Name = "PostgreSQL", NormalizedName = "POSTGRESQL" };
        repository.Skills.Add(skill);

        var result = await new LookupService(repository).DeleteAsync(
            LookupKind.Skill,
            skill.Id,
            CancellationToken.None);

        Assert.Equal(LookupServiceFailure.InUse, result);
        Assert.Single(repository.Skills);
    }

    private sealed class MemoryLookupRepository : ILookupRepository
    {
        public List<Skill> Skills { get; } = [];
        public List<Interest> Interests { get; } = [];
        public List<Category> Categories { get; } = [];
        public bool DeleteAllowed { get; init; } = true;
        public bool WasSaved { get; private set; }

        public Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Skill>>(Skills);
        public Task<IReadOnlyList<Interest>> GetInterestsAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Interest>>(Interests);
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Category>>(Categories);
        public Task<bool> SkillsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult(ids.All(id => Skills.Any(item => item.Id == id)));
        public Task<bool> InterestsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult(ids.All(id => Interests.Any(item => item.Id == id)));
        public Task<bool> CategoryExistsAsync(Guid id, CancellationToken token) => Task.FromResult(Categories.Any(item => item.Id == id));

        public Task<object?> FindAsync(LookupKind kind, Guid id, CancellationToken token) => Task.FromResult(kind switch
        {
            LookupKind.Skill => (object?)Skills.SingleOrDefault(item => item.Id == id),
            LookupKind.Interest => Interests.SingleOrDefault(item => item.Id == id),
            LookupKind.Category => Categories.SingleOrDefault(item => item.Id == id),
            _ => null
        });

        public Task<bool> NormalizedNameExistsAsync(LookupKind kind, string normalized, Guid? exceptId, CancellationToken token) =>
            Task.FromResult(All(kind).Any(item => item.Normalized == normalized && item.Id != exceptId));

        public Task AddAsync(LookupKind kind, string name, string normalized, CancellationToken token)
        {
            switch (kind)
            {
                case LookupKind.Skill: Skills.Add(new Skill { Name = name, NormalizedName = normalized }); break;
                case LookupKind.Interest: Interests.Add(new Interest { Name = name, NormalizedName = normalized }); break;
                case LookupKind.Category: Categories.Add(new Category { Name = name, NormalizedName = normalized }); break;
            }
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken token)
        {
            WasSaved = true;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(LookupKind kind, object entity, CancellationToken token) => Task.FromResult(DeleteAllowed);

        private IEnumerable<(Guid Id, string Normalized)> All(LookupKind kind) => kind switch
        {
            LookupKind.Skill => Skills.Select(item => (item.Id, item.NormalizedName)),
            LookupKind.Interest => Interests.Select(item => (item.Id, item.NormalizedName)),
            LookupKind.Category => Categories.Select(item => (item.Id, item.NormalizedName)),
            _ => []
        };
    }
}
