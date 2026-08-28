using System.Text.Json;
using SkillMatchBE.DTOs.Projects;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task Create_RejectsDuplicateNormalizedTitle()
    {
        var repository = new FakeProjectRepository { DuplicateTitle = true };
        var service = CreateService(repository);

        var result = await service.CreateAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(ProjectFailure.DuplicateTitle, result.Failure);
        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task Create_RejectsInvalidTeamSizeOrder()
    {
        var repository = new FakeProjectRepository();
        var request = ValidRequest() with { MinimumTeamSize = 5, PreferredTeamSize = 3, MaximumTeamSize = 4 };

        var result = await CreateService(repository).CreateAsync(request, CancellationToken.None);

        Assert.Equal(ProjectFailure.InvalidTeamSizes, result.Failure);
    }

    [Fact]
    public async Task Create_RequiresAtLeastOneSkill()
    {
        var result = await CreateService(new FakeProjectRepository()).CreateAsync(
            ValidRequest() with { RequiredSkillIds = [] }, CancellationToken.None);

        Assert.Equal(ProjectFailure.MissingRequiredSkills, result.Failure);
    }

    [Fact]
    public async Task PublishedProject_CannotBeDeleted()
    {
        var repository = new FakeProjectRepository { Existing = Project(ProjectStatus.Published) };

        var result = await CreateService(repository).DeleteAsync(repository.Existing.Id, CancellationToken.None);

        Assert.Equal(ProjectFailure.DeleteBlocked, result);
        Assert.False(repository.WasDeleted);
    }

    [Fact]
    public async Task StudentSearch_DelegatesFiltersAndNeverSerializesAdminNotes()
    {
        var repository = new FakeProjectRepository { SearchResults = [Project(ProjectStatus.Published)] };
        var query = new ProjectQuery("campus", SkillId, CategoryId, "Intermediate", true, 3);

        var results = await CreateService(repository).SearchAsync(query, CancellationToken.None);
        var json = JsonSerializer.Serialize(results);

        Assert.Same(query, repository.CapturedQuery);
        Assert.Single(results);
        Assert.DoesNotContain("AdminNotes", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private instructor note", json, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Guid SkillId = Guid.Parse("69fa9d46-dfe4-4c1d-9c0a-4247945774a9");
    private static readonly Guid CategoryId = Guid.Parse("82fe058e-71c2-487d-b68e-638dc54eaa32");

    private static ProjectService CreateService(FakeProjectRepository repository) =>
        new(repository, new FakeLookupRepository(), new TestClock());

    private static SaveProjectRequest ValidRequest() => new(
        "Campus Hub",
        "Build a useful collaboration experience for students.",
        "Intermediate",
        CategoryId,
        2,
        3,
        4,
        [SkillId],
        "private instructor note");

    private static ProjectTopic Project(ProjectStatus status) => new()
    {
        Title = "Campus Hub",
        NormalizedTitle = "CAMPUS HUB",
        Description = "Build a useful collaboration experience for students.",
        AdminNotes = "private instructor note",
        Difficulty = ProjectDifficulty.Intermediate,
        Status = status,
        MinimumTeamSize = 2,
        PreferredTeamSize = 3,
        MaximumTeamSize = 4,
        CategoryId = CategoryId,
        Category = new Category { Id = CategoryId, Name = "Education", NormalizedName = "EDUCATION" },
        CreatedAt = DateTimeOffset.Parse("2026-08-27T00:00:00Z"),
        UpdatedAt = DateTimeOffset.Parse("2026-08-27T00:00:00Z"),
        RequiredSkills =
        [
            new ProjectRequiredSkill
            {
                SkillId = SkillId,
                Skill = new Skill { Id = SkillId, Name = "React", NormalizedName = "REACT" }
            }
        ]
    };

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public bool DuplicateTitle { get; init; }
        public ProjectTopic? Existing { get; init; }
        public IReadOnlyList<ProjectTopic> SearchResults { get; init; } = [];
        public ProjectQuery? CapturedQuery { get; private set; }
        public ProjectTopic? Added { get; private set; }
        public bool WasDeleted { get; private set; }

        public Task<IReadOnlyList<ProjectTopic>> SearchPublishedAsync(ProjectQuery query, CancellationToken cancellationToken)
        { CapturedQuery = query; return Task.FromResult(SearchResults); }
        public Task<IReadOnlyList<ProjectTopic>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ProjectTopic>>(Existing is null ? [] : [Existing]);
        public Task<ProjectTopic?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Existing?.Id == id ? Existing : null);
        public Task<bool> NormalizedTitleExistsAsync(string normalizedTitle, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(DuplicateTitle);
        public Task AddAsync(ProjectTopic project, CancellationToken cancellationToken) { Added = project; return Task.CompletedTask; }
        public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(ProjectTopic project, CancellationToken cancellationToken) { WasDeleted = true; return Task.CompletedTask; }
    }

    private sealed class FakeLookupRepository : ILookupRepository
    {
        public Task<bool> SkillsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult(true);
        public Task<bool> InterestsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult(true);
        public Task<bool> CategoryExistsAsync(Guid id, CancellationToken token) => Task.FromResult(true);
        public Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Skill>>([]);
        public Task<IReadOnlyList<Interest>> GetInterestsAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Interest>>([]);
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<object?> FindAsync(LookupKind kind, Guid id, CancellationToken token) => Task.FromResult<object?>(null);
        public Task<bool> NormalizedNameExistsAsync(LookupKind kind, string name, Guid? id, CancellationToken token) => Task.FromResult(false);
        public Task AddAsync(LookupKind kind, string name, string normalized, CancellationToken token) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken token) => Task.CompletedTask;
        public Task<bool> DeleteAsync(LookupKind kind, object entity, CancellationToken token) => Task.FromResult(true);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-08-27T00:00:00Z");
    }
}
