using SkillMatchBE.DTOs.Profiles;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class ProfileServiceTests
{
    private static readonly Guid UserId = Guid.Parse("5d8f53d4-6ca1-4c6e-a577-36ca8792df73");
    private static readonly Guid ReactId = Guid.Parse("5953b317-e646-4459-8e29-0d37f5f0b6c8");
    private static readonly Guid AiId = Guid.Parse("868357bd-69d2-42c1-b342-3db51af31606");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-29T12:00:00Z");

    [Fact]
    public async Task Get_MissingProfileReturnsEmptyCompletenessChecklistForExistingStudent()
    {
        var service = CreateService(new MemoryProfileRepository(), new FakeLookupRepository(), out _);

        var result = await service.GetAsync(UserId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.CompletenessPercent);
        Assert.Equal(5, result.MissingFields.Count);
        Assert.Equal("student@skillmatch.local", result.Email);
    }

    [Fact]
    public async Task Update_RejectsInvalidExperienceBeforeSaving()
    {
        var repository = new MemoryProfileRepository();
        var service = CreateService(repository, new FakeLookupRepository(), out _);

        var result = await service.UpdateAsync(
            UserId,
            ValidRequest() with { ExperienceLevel = "Expert" },
            CancellationToken.None);

        Assert.Equal(ProfileFailure.InvalidExperienceLevel, result.Failure);
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task Update_RejectsUnknownSkillOrInterest()
    {
        var lookups = new FakeLookupRepository { AllIdsExist = false };
        var service = CreateService(new MemoryProfileRepository(), lookups, out _);

        var result = await service.UpdateAsync(UserId, ValidRequest(), CancellationToken.None);

        Assert.Equal(ProfileFailure.InvalidLookup, result.Failure);
    }

    [Fact]
    public async Task Update_TrimsTechnologiesSynchronizesLookupsAndReturnsCompleteProfile()
    {
        var repository = new MemoryProfileRepository();
        var service = CreateService(repository, new FakeLookupRepository(), out _);
        var request = ValidRequest() with
        {
            PreferredTechnologies = [" React ", "react", " PostgreSQL ", " "],
            SkillIds = [ReactId, ReactId],
            InterestIds = [AiId, AiId]
        };

        var result = await service.UpdateAsync(UserId, request, CancellationToken.None);

        Assert.Equal(ProfileFailure.None, result.Failure);
        Assert.NotNull(repository.Saved);
        Assert.Equal(Now, repository.Saved.UpdatedAt);
        Assert.Equal(["React", "PostgreSQL"], repository.Saved.PreferredTechnologies);
        Assert.Single(repository.Saved.Skills);
        Assert.Single(repository.Saved.Interests);
        Assert.Equal(100, result.Profile?.CompletenessPercent);
        Assert.Empty(result.Profile?.MissingFields ?? []);
    }

    private static UpdateStudentProfileRequest ValidRequest() => new(
        "Intermediate",
        "Build accessible software that helps students collaborate.",
        ["React"],
        [ReactId],
        [AiId]);

    private static ProfileService CreateService(
        MemoryProfileRepository profiles,
        FakeLookupRepository lookups,
        out MemoryUserRepository users)
    {
        users = new MemoryUserRepository();
        profiles.Lookups = lookups;
        return new ProfileService(profiles, lookups, users, new TestClock());
    }

    private sealed class MemoryProfileRepository : IProfileRepository
    {
        public FakeLookupRepository Lookups { get; set; } = null!;
        public StudentProfile? Saved { get; private set; }

        public Task<StudentProfile?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Saved?.UserId == userId ? Saved : null);

        public Task SaveAsync(StudentProfile profile, CancellationToken cancellationToken)
        {
            foreach (var item in profile.Skills)
                item.Skill = Lookups.Skills.Single(skill => skill.Id == item.SkillId);
            foreach (var item in profile.Interests)
                item.Interest = Lookups.Interests.Single(interest => interest.Id == item.InterestId);
            Saved = profile;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryUserRepository : IUserRepository
    {
        private readonly ApplicationUser user = new()
        {
            Id = UserId,
            Email = "student@skillmatch.local",
            NormalizedEmail = "STUDENT@SKILLMATCH.LOCAL",
            PasswordHash = "hashed",
            Role = UserRole.Student,
            CreatedAt = Now
        };

        public Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult(id == user.Id ? user : null);
        public Task<ApplicationUser?> FindByNormalizedEmailAsync(string email, CancellationToken token) => Task.FromResult<ApplicationUser?>(null);
        public Task<bool> TryAddAsync(ApplicationUser value, CancellationToken token) => Task.FromResult(false);
        public Task UpdateAsync(ApplicationUser value, CancellationToken token) => Task.CompletedTask;
    }

    private sealed class FakeLookupRepository : ILookupRepository
    {
        public bool AllIdsExist { get; init; } = true;
        public IReadOnlyList<Skill> Skills { get; } =
            [new Skill { Id = ReactId, Name = "React", NormalizedName = "REACT" }];
        public IReadOnlyList<Interest> Interests { get; } =
            [new Interest { Id = AiId, Name = "AI", NormalizedName = "AI" }];

        public Task<bool> SkillsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult(AllIdsExist);
        public Task<bool> InterestsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult(AllIdsExist);
        public Task<bool> CategoryExistsAsync(Guid id, CancellationToken token) => Task.FromResult(AllIdsExist);
        public Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken token) => Task.FromResult(Skills);
        public Task<IReadOnlyList<Interest>> GetInterestsAsync(CancellationToken token) => Task.FromResult(Interests);
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task<object?> FindAsync(LookupKind kind, Guid id, CancellationToken token) => Task.FromResult<object?>(null);
        public Task<bool> NormalizedNameExistsAsync(LookupKind kind, string name, Guid? id, CancellationToken token) => Task.FromResult(false);
        public Task AddAsync(LookupKind kind, string name, string normalized, CancellationToken token) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken token) => Task.CompletedTask;
        public Task<bool> DeleteAsync(LookupKind kind, object entity, CancellationToken token) => Task.FromResult(true);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
