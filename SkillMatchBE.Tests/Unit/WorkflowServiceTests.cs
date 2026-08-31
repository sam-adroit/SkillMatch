using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.DTOs.Projects;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class WorkflowServiceTests
{
    [Fact]
    public async Task Apply_RequiresSavedProfile()
    {
        var fixture = new Fixture { HasProfile = false };

        var result = await fixture.Service.ApplyAsync(StudentId, ProjectId, new("Ready to contribute"), default);

        Assert.Equal(WorkflowFailure.MissingProfile, result.Failure);
    }

    [Fact]
    public async Task Apply_BlocksDuplicateAndClosedProject()
    {
        var fixture = new Fixture();
        fixture.Repository.Applications.Add(fixture.Application(ApplicationStatus.Pending));
        var duplicate = await fixture.Service.ApplyAsync(StudentId, ProjectId, new(null), default);
        fixture.Repository.Applications.Clear();
        fixture.Project.Status = ProjectStatus.Closed;
        var closed = await fixture.Service.ApplyAsync(StudentId, ProjectId, new(null), default);

        Assert.Equal(WorkflowFailure.DuplicateApplication, duplicate.Failure);
        Assert.Equal(WorkflowFailure.ProjectClosed, closed.Failure);
    }

    [Fact]
    public async Task Approve_EnforcesCapacityBoundary()
    {
        var fixture = new Fixture();
        var application = fixture.Application(ApplicationStatus.Pending);
        fixture.Repository.Applications.Add(application);
        fixture.Repository.ApprovedCount = fixture.Project.MaximumTeamSize;

        var result = await fixture.Service.DecideAsync(application.Id, new("Approved", null), default);

        Assert.Equal(WorkflowFailure.CapacityReached, result.Failure);
        Assert.Equal(ApplicationStatus.Pending, application.Status);
    }

    [Fact]
    public async Task Approve_BlocksStudentAlreadyAssignedInCycle()
    {
        var fixture = new Fixture();
        var application = fixture.Application(ApplicationStatus.Pending);
        fixture.Repository.Applications.Add(application);
        fixture.Repository.HasActiveTeamResult = true;

        var result = await fixture.Service.DecideAsync(application.Id, new("Approved", "Looks good"), default);

        Assert.Equal(WorkflowFailure.ExistingAssignment, result.Failure);
    }

    [Fact]
    public async Task Decide_ApprovesPendingApplicationAndRecordsDecisionTime()
    {
        var fixture = new Fixture();
        var application = fixture.Application(ApplicationStatus.Pending);
        fixture.Repository.Applications.Add(application);

        var result = await fixture.Service.DecideAsync(application.Id, new("Approved", "Strong fit"), default);

        Assert.Equal(WorkflowFailure.None, result.Failure);
        Assert.Equal("Approved", result.Value?.Status);
        Assert.Equal(FixedNow, result.Value?.DecidedAt);
    }

    [Fact]
    public async Task GetStudentApplications_PreservesClosedProjectHistoryAndStatus()
    {
        var fixture = new Fixture();
        var application = fixture.Application(ApplicationStatus.Rejected);
        application.DecisionNote = "Capacity was reached";
        application.DecidedAt = FixedNow;
        fixture.Repository.Applications.Add(application);
        fixture.Project.Status = ProjectStatus.Closed;

        var result = await fixture.Service.GetStudentApplicationsAsync(StudentId, default);

        var response = Assert.Single(result);
        Assert.Equal("Closed", response.ProjectStatus);
        Assert.Equal("Rejected", response.Status);
        Assert.Equal("Capacity was reached", response.DecisionNote);
        Assert.Equal(FixedNow, response.DecidedAt);
        Assert.Equal("Test Student", response.StudentName);
    }

    [Fact]
    public async Task CreateTeam_RequiresApprovedMembersAndLeaderInMembership()
    {
        var fixture = new Fixture();
        var unapproved = await fixture.Service.CreateTeamAsync(
            new(ProjectId, "Alpha", StudentId, [StudentId]), default);
        fixture.Repository.ApprovedStudents.Add(StudentId);
        var leaderMissing = await fixture.Service.CreateTeamAsync(
            new(ProjectId, "Alpha", OtherStudentId, [StudentId]), default);

        Assert.Equal(WorkflowFailure.ApplicationNotApproved, unapproved.Failure);
        Assert.Equal(WorkflowFailure.LeaderNotMember, leaderMissing.Failure);
    }

    [Fact]
    public async Task CreateTeam_EnforcesCapacityAndOneActiveTeamPerCycle()
    {
        var fixture = new Fixture();
        fixture.Repository.ApprovedStudents.UnionWith([StudentId, OtherStudentId]);
        fixture.Project.MaximumTeamSize = 1;
        var full = await fixture.Service.CreateTeamAsync(
            new(ProjectId, "Alpha", StudentId, [StudentId, OtherStudentId]), default);
        fixture.Project.MaximumTeamSize = 4;
        fixture.Repository.HasActiveTeamResult = true;
        var assigned = await fixture.Service.CreateTeamAsync(
            new(ProjectId, "Alpha", StudentId, [StudentId]), default);

        Assert.Equal(WorkflowFailure.CapacityReached, full.Failure);
        Assert.Equal(WorkflowFailure.ExistingAssignment, assigned.Failure);
    }

    [Fact]
    public async Task CreateTeam_SucceedsWithApprovedAvailableMembers()
    {
        var fixture = new Fixture();
        fixture.Repository.ApprovedStudents.UnionWith([StudentId, OtherStudentId]);

        var result = await fixture.Service.CreateTeamAsync(
            new(ProjectId, "Alpha", StudentId, [StudentId, OtherStudentId]), default);

        Assert.Equal(WorkflowFailure.None, result.Failure);
        Assert.Equal(2, result.Value?.Members.Count);
        Assert.Single(result.Value!.Members, member => member.IsLeader);
        Assert.Contains(result.Value.Members, member => member.Name == "Test Student");
        Assert.Contains(result.Value.Members, member => member.Name == "Other Student");
    }

    private static readonly Guid StudentId = Guid.Parse("c11387aa-8129-4f37-9841-0713177c1d39");
    private static readonly Guid OtherStudentId = Guid.Parse("166b8afc-0865-4791-83bd-8bcb7d233e75");
    private static readonly Guid ProjectId = Guid.Parse("82fe058e-71c2-487d-b68e-638dc54eaa32");
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-08-27T20:00:00Z");

    private sealed class Fixture
    {
        public bool HasProfile { get; set; } = true;
        public ProjectTopic Project { get; } = new()
        {
            Id = ProjectId, Title = "Campus Hub", NormalizedTitle = "CAMPUS HUB",
            Description = "A sufficiently descriptive project for workflow tests.", Status = ProjectStatus.Published,
            MaximumTeamSize = 2, MinimumTeamSize = 1, PreferredTeamSize = 2,
            CreatedAt = FixedNow, UpdatedAt = FixedNow
        };
        public FakeRepository Repository { get; }
        public WorkflowService Service { get; }

        public Fixture()
        {
            Repository = new FakeRepository(Project);
            Service = new WorkflowService(Repository, new FakeProjectRepository(Project), new FakeProfileRepository(this), new TestClock());
        }

        public ProjectApplication Application(ApplicationStatus status) => new()
        {
            StudentId = StudentId, Student = User(StudentId), ProjectId = ProjectId, Project = Project,
            Status = status, AppliedAt = FixedNow
        };

        public static ApplicationUser User(Guid id) => new()
        {
            FirstName = id == StudentId ? "Test" : "Other",
            LastName = "Student",
            Id = id, Email = $"{id:N}@example.edu", NormalizedEmail = $"{id:N}@EXAMPLE.EDU",
            PasswordHash = "unused", CreatedAt = FixedNow
        };
    }

    private sealed class FakeRepository(ProjectTopic project) : IApplicationTeamRepository
    {
        public List<ProjectApplication> Applications { get; } = [];
        public List<Team> Teams { get; } = [];
        public HashSet<Guid> ApprovedStudents { get; } = [];
        public int ApprovedCount { get; set; }
        public bool HasActiveTeamResult { get; set; }

        public Task<T> InSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken token) => action(token);
        public Task<ProjectApplication?> GetApplicationAsync(Guid id, CancellationToken token) => Task.FromResult(Applications.SingleOrDefault(item => item.Id == id));
        public Task<ProjectApplication?> GetApplicationAsync(Guid studentId, Guid projectId, CancellationToken token) => Task.FromResult(Applications.SingleOrDefault(item => item.StudentId == studentId && item.ProjectId == projectId));
        public Task<IReadOnlyList<ProjectApplication>> GetStudentApplicationsAsync(Guid studentId, CancellationToken token) => Task.FromResult<IReadOnlyList<ProjectApplication>>(Applications.Where(item => item.StudentId == studentId).ToArray());
        public Task<IReadOnlyList<ProjectApplication>> GetApplicationsAsync(ApplicationQuery query, CancellationToken token) => Task.FromResult<IReadOnlyList<ProjectApplication>>(Applications);
        public Task<int> CountApprovedApplicationsAsync(Guid projectId, Guid? exceptId, CancellationToken token) => Task.FromResult(ApprovedCount);
        public Task AddApplicationAsync(ProjectApplication application, CancellationToken token) { application.Student = Fixture.User(application.StudentId); application.Project = project; Applications.Add(application); return Task.CompletedTask; }
        public Task<Team?> GetTeamAsync(Guid id, CancellationToken token) => Task.FromResult(Teams.SingleOrDefault(item => item.Id == id));
        public Task<Team?> GetTeamForProjectAsync(Guid projectId, CancellationToken token) => Task.FromResult(Teams.SingleOrDefault(item => item.ProjectId == projectId));
        public Task<IReadOnlyList<Team>> GetTeamsAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<Team>>(Teams);
        public Task<IReadOnlyList<Team>> GetStudentTeamsAsync(Guid studentId, CancellationToken token) => Task.FromResult<IReadOnlyList<Team>>(Teams.Where(team => team.Members.Any(member => member.StudentId == studentId)).ToArray());
        public Task<bool> HasActiveTeamAsync(Guid studentId, Guid? exceptTeamId, CancellationToken token) => Task.FromResult(HasActiveTeamResult);
        public Task<bool> HasApprovedApplicationAsync(Guid studentId, Guid projectId, CancellationToken token) => Task.FromResult(ApprovedStudents.Contains(studentId));
        public Task<bool> IsTeamMemberAsync(Guid studentId, Guid teamId, CancellationToken token) => Task.FromResult(Teams.Any(team => team.Id == teamId && team.Members.Any(member => member.StudentId == studentId)));
        public Task AddTeamAsync(Team team, CancellationToken token) { team.Project = project; foreach (var member in team.Members) member.Student = Fixture.User(member.StudentId); Teams.Add(team); return Task.CompletedTask; }
        public Task SaveAsync(CancellationToken token) => Task.CompletedTask;
        public Task<DashboardCounts> GetDashboardCountsAsync(CancellationToken token) => Task.FromResult(new DashboardCounts(2, 1, Teams.Count, 0, 2 - Teams.SelectMany(team => team.Members).Count()));
    }

    private sealed class FakeProjectRepository(ProjectTopic project) : IProjectRepository
    {
        public Task<ProjectTopic?> GetAsync(Guid id, CancellationToken token) => Task.FromResult(id == project.Id ? project : null);
        public Task<IReadOnlyList<ProjectTopic>> SearchPublishedAsync(ProjectQuery query, CancellationToken token) => Task.FromResult<IReadOnlyList<ProjectTopic>>([]);
        public Task<IReadOnlyList<ProjectTopic>> GetAllAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<ProjectTopic>>([project]);
        public Task<bool> NormalizedTitleExistsAsync(string title, Guid? id, CancellationToken token) => Task.FromResult(false);
        public Task AddAsync(ProjectTopic value, CancellationToken token) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken token) => Task.CompletedTask;
        public Task DeleteAsync(ProjectTopic value, CancellationToken token) => Task.CompletedTask;
    }

    private sealed class FakeProfileRepository(Fixture fixture) : IProfileRepository
    {
        public Task<StudentProfile?> GetAsync(Guid userId, CancellationToken token) => Task.FromResult(fixture.HasProfile ? new StudentProfile { UserId = userId, Goals = "Goals" } : null);
        public Task SaveAsync(StudentProfile profile, CancellationToken token) => Task.CompletedTask;
    }

    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => FixedNow; }
}
