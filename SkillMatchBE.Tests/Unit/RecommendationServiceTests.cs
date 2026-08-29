using Microsoft.Extensions.Logging.Abstractions;
using SkillMatchBE.DTOs.Recommendations;
using SkillMatchBE.Entities;
using SkillMatchBE.Recommendations;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class RecommendationServiceTests
{
    [Fact]
    public async Task Projects_RanksNamedWeightsAndBreaksTiesStably()
    {
        var fixture = new Fixture();
        fixture.Repository.Projects.Add(Project("Zulu", "Education", ProjectDifficulty.Intermediate, "C#"));
        fixture.Repository.Projects.Add(Project("Alpha", "Education", ProjectDifficulty.Intermediate, "C#"));

        var result = await fixture.Service.RecommendProjectsAsync(StudentId, default);

        Assert.Equal(RecommendationFailure.None, result.Failure);
        Assert.Equal(["Alpha", "Zulu"], result.Value!.Results.Select(item => item.ProjectTitle));
        Assert.All(result.Value.Results, item => Assert.Equal(100m, item.Score));
    }

    [Fact]
    public async Task Projects_RequiresSufficientSavedProfile()
    {
        var missing = new Fixture { Profile = null };
        var insufficient = new Fixture();
        insufficient.Profile!.Interests.Clear();

        var missingResult = await missing.Service.RecommendProjectsAsync(StudentId, default);
        var insufficientResult = await insufficient.Service.RecommendProjectsAsync(StudentId, default);

        Assert.Equal(RecommendationFailure.MissingProfile, missingResult.Failure);
        Assert.Equal(RecommendationFailure.InsufficientProfile, insufficientResult.Failure);
    }

    [Fact]
    public async Task Projects_ReturnsClearNoResultsWithoutCallingProvider()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.RecommendProjectsAsync(StudentId, default);

        Assert.Empty(result.Value!.Results);
        Assert.Equal("NoResults", result.Value.ProviderStatus);
        Assert.Equal(0, fixture.Provider.CallCount);
    }

    [Fact]
    public async Task Projects_StoresAiMetadataAndReusesCurrentBatch()
    {
        var fixture = new Fixture();
        fixture.Repository.Projects.Add(Project("Campus Hub", "Education", ProjectDifficulty.Intermediate, "C#"));

        var first = await fixture.Service.RecommendProjectsAsync(StudentId, default);
        var second = await fixture.Service.RecommendProjectsAsync(StudentId, default);

        Assert.Equal("AiGenerated", first.Value!.ProviderStatus);
        Assert.Equal("OpenAI", first.Value.Results[0].Provider);
        Assert.Equal("gpt-test", first.Value.Results[0].Model);
        Assert.Single(fixture.Repository.History);
        Assert.True(second.Value!.Reused);
        Assert.Equal(1, fixture.Provider.CallCount);
    }

    [Fact]
    public async Task Projects_UsesVisibleFallbackWhenProviderFails()
    {
        var fixture = new Fixture();
        fixture.Repository.Projects.Add(Project("Campus Hub", "Education", ProjectDifficulty.Intermediate, "Python"));
        fixture.Provider.Exception = new HttpRequestException("outage");

        var result = await fixture.Service.RecommendProjectsAsync(StudentId, default);

        var recommendation = Assert.Single(result.Value!.Results);
        Assert.Equal("Fallback", recommendation.ProviderStatus);
        Assert.Equal("Deterministic", recommendation.Provider);
        Assert.Contains("grow", recommendation.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RecommendationProviderStatus.Fallback, Assert.Single(fixture.Repository.History).ProviderStatus);
    }

    [Fact]
    public async Task Projects_DoesNotCacheFallbackBatch()
    {
        var fixture = new Fixture();
        fixture.Repository.Projects.Add(Project("Campus Hub", "Education", ProjectDifficulty.Intermediate, "C#"));
        fixture.Provider.Exception = new HttpRequestException("temporary outage");
        await fixture.Service.RecommendProjectsAsync(StudentId, default);
        fixture.Provider.Exception = null;

        var recovered = await fixture.Service.RecommendProjectsAsync(StudentId, default);

        Assert.False(recovered.Value!.Reused);
        Assert.Equal("AiGenerated", recovered.Value.ProviderStatus);
        Assert.Equal(2, fixture.Provider.CallCount);
    }

    [Fact]
    public async Task Teammates_ExcludesSelfInactiveAndAssignedStudentsAndReturnsNoPrivateFields()
    {
        var fixture = new Fixture();
        fixture.Repository.AvailableProfiles.Add(fixture.Profile!);
        fixture.Repository.AvailableProfiles.Add(Profile(OtherStudentId, true, "C#", "UX Design"));
        fixture.Repository.AvailableProfiles.Add(Profile(Guid.NewGuid(), false, "C#"));
        var assigned = Profile(Guid.NewGuid(), true, "Python");
        assigned.User.TeamMemberships.Add(new TeamMember { StudentId = assigned.UserId, Team = new Team { Name = "Assigned", Status = TeamStatus.Active } });
        fixture.Repository.AvailableProfiles.Add(assigned);

        var result = await fixture.Service.SuggestTeammatesAsync(StudentId, default);

        var teammate = Assert.Single(result.Value!);
        Assert.Equal(OtherStudentId, teammate.StudentId);
        Assert.Contains("UX Design", teammate.ComplementarySkills);
        Assert.DoesNotContain("@", teammate.DisplayName);
        Assert.DoesNotContain(typeof(TeammateSuggestionResponse).GetProperties(), property => property.Name.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SkillGaps_UsesRequiredSkillsMinusMemberSkillsAndEnforcesOwnership()
    {
        var fixture = new Fixture();
        var team = TeamWithSkills("C#", "PostgreSQL");
        fixture.Repository.Team = team;

        var forbidden = await fixture.Service.GetTeamSkillGapsAsync(team.Id, OtherStudentId, false, default);
        var allowed = await fixture.Service.GetTeamSkillGapsAsync(team.Id, StudentId, false, default);

        Assert.Equal(RecommendationFailure.Forbidden, forbidden.Failure);
        Assert.Equal(["C#"], allowed.Value!.CoveredSkills);
        Assert.Equal(["PostgreSQL"], allowed.Value.MissingSkills);
    }

    private static readonly Guid StudentId = Guid.Parse("8579830b-d88f-49f0-b0cb-43f43a0ad2a6");
    private static readonly Guid OtherStudentId = Guid.Parse("ce78593c-a462-46af-8e10-789c4ec1bc98");
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-08-28T20:00:00Z");

    private sealed class Fixture
    {
        private StudentProfile? profile = Profile(StudentId, true, "C#");
        public StudentProfile? Profile { get => profile; set { profile = value; Repository.Profile = value; } }
        public FakeRepository Repository { get; }
        public FakeProvider Provider { get; } = new();
        public RecommendationService Service { get; }

        public Fixture()
        {
            Repository = new FakeRepository { Profile = profile };
            Service = new(Repository, Provider, new TestClock(), NullLogger<RecommendationService>.Instance);
        }
    }

    private static StudentProfile Profile(Guid id, bool active, params string[] skills)
    {
        var user = new ApplicationUser
        {
            Id = id,
            Email = $"private-{id:N}@example.edu",
            NormalizedEmail = $"PRIVATE-{id:N}@EXAMPLE.EDU",
            PasswordHash = "private-password-hash",
            Role = UserRole.Student,
            IsActive = active,
            CreatedAt = FixedNow
        };
        var profile = new StudentProfile
        {
            UserId = id,
            User = user,
            ExperienceLevel = ExperienceLevel.Intermediate,
            Goals = "Private goals must never leave the service.",
            PreferredTechnologies = ["C#"],
            UpdatedAt = FixedNow
        };
        user.StudentProfile = profile;
        foreach (var name in skills)
            profile.Skills.Add(new StudentProfileSkill { Profile = profile, ProfileUserId = id, Skill = Skill(name), SkillId = Skill(name).Id });
        var interest = new Interest { Name = "Education", NormalizedName = "EDUCATION" };
        profile.Interests.Add(new StudentProfileInterest { Profile = profile, ProfileUserId = id, Interest = interest, InterestId = interest.Id });
        return profile;
    }

    private static ProjectTopic Project(string title, string categoryName, ProjectDifficulty difficulty, params string[] skills)
    {
        var category = new Category { Name = categoryName, NormalizedName = categoryName.ToUpperInvariant() };
        var project = new ProjectTopic
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Description = "A sufficiently detailed recommendation test project.",
            Category = category,
            CategoryId = category.Id,
            Difficulty = difficulty,
            Status = ProjectStatus.Published,
            MinimumTeamSize = 1,
            PreferredTeamSize = 2,
            MaximumTeamSize = 4,
            CreatedAt = FixedNow,
            UpdatedAt = FixedNow
        };
        foreach (var name in skills)
        {
            var skill = Skill(name);
            project.RequiredSkills.Add(new ProjectRequiredSkill { Project = project, ProjectId = project.Id, Skill = skill, SkillId = skill.Id });
        }
        return project;
    }

    private static Team TeamWithSkills(params string[] requiredSkills)
    {
        var project = Project("Team Project", "Education", ProjectDifficulty.Intermediate, requiredSkills);
        var memberProfile = Profile(StudentId, true, "C#");
        var team = new Team { Project = project, ProjectId = project.Id, Name = "Alpha", LeaderStudentId = StudentId, Status = TeamStatus.Active };
        team.Members.Add(new TeamMember { Team = team, TeamId = team.Id, Student = memberProfile.User, StudentId = StudentId, JoinedAt = FixedNow });
        return team;
    }

    private static Skill Skill(string name) => new() { Name = name, NormalizedName = name.ToUpperInvariant() };

    private sealed class FakeProvider : IRecommendationProvider
    {
        public int CallCount { get; private set; }
        public Exception? Exception { get; set; }
        public Task<RecommendationProviderResult> GenerateProjectExplanationsAsync(RecommendationExplanationRequest request, CancellationToken token)
        {
            CallCount++;
            if (Exception is not null) throw Exception;
            return Task.FromResult(new RecommendationProviderResult(
                request.Projects.ToDictionary(item => item.ProjectId, item => $"AI explanation for {item.Title}"),
                "OpenAI",
                "gpt-test"));
        }
    }

    private sealed class FakeRepository : IRecommendationRepository
    {
        public StudentProfile? Profile { get; set; }
        public List<ProjectTopic> Projects { get; } = [];
        public List<RecommendationHistory> History { get; } = [];
        public List<StudentProfile> AvailableProfiles { get; } = [];
        public Team? Team { get; set; }

        public Task<StudentProfile?> GetProfileAsync(Guid id, CancellationToken token) => Task.FromResult(Profile);
        public Task<IReadOnlyList<ProjectTopic>> GetPublishedProjectsAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<ProjectTopic>>(Projects);
        public Task<IReadOnlyList<RecommendationHistory>> GetHistoryAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyList<RecommendationHistory>>(History.OrderByDescending(item => item.CreatedAt).ToArray());
        public Task<IReadOnlyDictionary<Guid, string>> GetProjectTitlesAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult<IReadOnlyDictionary<Guid, string>>(Projects.Where(item => ids.Contains(item.Id)).ToDictionary(item => item.Id, item => item.Title));
        public Task AddHistoryAsync(IReadOnlyCollection<RecommendationHistory> history, CancellationToken token) { History.AddRange(history); return Task.CompletedTask; }
        public Task<IReadOnlyList<StudentProfile>> GetAvailableProfilesAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyList<StudentProfile>>(AvailableProfiles);
        public Task<Team?> GetTeamWithProfilesAsync(Guid id, CancellationToken token) => Task.FromResult(Team?.Id == id ? Team : null);
    }

    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => FixedNow; }
}
