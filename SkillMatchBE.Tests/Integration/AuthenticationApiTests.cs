using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SkillMatchBE.Auth;
using SkillMatchBE.DTOs.Auth;
using SkillMatchBE.DTOs.Recommendations;
using SkillMatchBE.Entities;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Integration;

public sealed class AuthenticationApiTests : IClassFixture<AuthenticationApiFactory>
{
    private readonly AuthenticationApiFactory factory;

    public AuthenticationApiTests(AuthenticationApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task InvalidLogin_ReturnsUnauthorizedProblemDetails()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("student@example.edu", "incorrect-password"));

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized,
            await response.Content.ReadAsStringAsync());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("not-a-jwt")]
    public async Task InvalidToken_CannotAccessCurrentUser(string token)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_CannotAccessCurrentUser()
    {
        var token = factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow.AddHours(-2));
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StudentToken_CanAccessCurrentUser()
    {
        var token = factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow);
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal("Student", user?.Role);
    }

    [Fact]
    public async Task StudentToken_IsForbiddenFromAdminEndpoint()
    {
        var token = factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow);
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.GetAsync("/api/admin/auth-check");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanAccessAdminEndpoint()
    {
        var token = factory.CreateToken(UserRole.Admin, DateTimeOffset.UtcNow);
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.GetAsync("/api/admin/auth-check");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AnonymousUser_CannotAccessStudentProfile()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CannotAccessStudentProfile()
    {
        using var client = CreateAuthenticatedClient(factory.CreateToken(UserRole.Admin, DateTimeOffset.UtcNow));
        using var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StudentToken_CannotManageProjects()
    {
        using var client = CreateAuthenticatedClient(factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow));
        using var response = await client.PostAsJsonAsync("/api/admin/projects", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotViewApplications()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/applications/967f841d-933d-4e62-b2fc-2f4218e7464e/decision", "PATCH")]
    [InlineData("/api/admin/teams", "POST")]
    public async Task StudentToken_CannotManageApplicationOrTeamWorkflows(string path, string method)
    {
        using var client = CreateAuthenticatedClient(factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow));
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new { })
        };
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StudentToken_CannotViewAdminDashboard()
    {
        using var client = CreateAuthenticatedClient(factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow));
        using var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotRequestRecommendations()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/api/recommendations/projects", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CannotRequestStudentRecommendations()
    {
        using var client = CreateAuthenticatedClient(factory.CreateToken(UserRole.Admin, DateTimeOffset.UtcNow));
        using var response = await client.PostAsync("/api/recommendations/projects", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StudentToken_CanRequestRecommendationsAndTeammates()
    {
        using var client = CreateAuthenticatedClient(factory.CreateToken(UserRole.Student, DateTimeOffset.UtcNow));
        using var projects = await client.PostAsync("/api/recommendations/projects", null);
        using var teammates = await client.GetAsync("/api/recommendations/teammates");

        projects.EnsureSuccessStatusCode();
        teammates.EnsureSuccessStatusCode();
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AuthenticationApiFactory : WebApplicationFactory<Program>
{
    private const string SigningKey = "test-only-signing-key-at-least-32-bytes-long";
    private static readonly Guid UserId = Guid.Parse("c11387aa-8129-4f37-9841-0713177c1d39");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=127.0.0.1;Port=1;Database=skillmatch_test;Username=test;Password=test");
        builder.UseSetting("Jwt:Issuer", "SkillMatchBE.Tests");
        builder.UseSetting("Jwt:Audience", "SkillMatchFE.Tests");
        builder.UseSetting("Jwt:Key", SigningKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(new StubAuthService(UserId));
            services.RemoveAll<IRecommendationService>();
            services.AddSingleton<IRecommendationService>(new StubRecommendationService());
        });
    }

    public string CreateToken(UserRole role, DateTimeOffset issuedAt)
    {
        var clock = new TestClock(issuedAt);
        var options = Options.Create(new JwtOptions
        {
            Issuer = "SkillMatchBE.Tests",
            Audience = "SkillMatchFE.Tests",
            Key = SigningKey,
            ExpiresMinutes = 60
        });
        var service = new JwtTokenService(options, clock);
        return service.CreateToken(new ApplicationUser
        {
            Id = UserId,
            Email = "user@example.edu",
            NormalizedEmail = "USER@EXAMPLE.EDU",
            PasswordHash = "unused",
            Role = role,
            CreatedAt = issuedAt
        }).Value;
    }

    private sealed class StubAuthService(Guid userId) : IAuthService
    {
        public Task<AuthServiceResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(AuthServiceResult.Failed(AuthFailure.DuplicateEmail));

        public Task<AuthServiceResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(AuthServiceResult.Failed(AuthFailure.InvalidCredentials));

        public Task<CurrentUserResponse?> GetCurrentUserAsync(Guid id, CancellationToken cancellationToken)
        {
            CurrentUserResponse? response = id == userId
                ? new CurrentUserResponse(id, "user@example.edu", "Student")
                : null;
            return Task.FromResult(response);
        }
    }

    private sealed class TestClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow => value;
    }

    private sealed class StubRecommendationService : IRecommendationService
    {
        public Task<RecommendationResult<RecommendationBatchResponse>> RecommendProjectsAsync(Guid id, CancellationToken token) =>
            Task.FromResult(RecommendationResult<RecommendationBatchResponse>.Success(new([], false, "NoResults")));
        public Task<RecommendationResult<IReadOnlyList<RecommendationHistoryResponse>>> GetHistoryAsync(Guid id, CancellationToken token) =>
            Task.FromResult(RecommendationResult<IReadOnlyList<RecommendationHistoryResponse>>.Success([]));
        public Task<RecommendationResult<IReadOnlyList<TeammateSuggestionResponse>>> SuggestTeammatesAsync(Guid id, CancellationToken token) =>
            Task.FromResult(RecommendationResult<IReadOnlyList<TeammateSuggestionResponse>>.Success([]));
        public Task<RecommendationResult<TeamSkillGapResponse>> GetTeamSkillGapsAsync(Guid teamId, Guid userId, bool isAdmin, CancellationToken token) =>
            Task.FromResult(RecommendationResult<TeamSkillGapResponse>.Fail(RecommendationFailure.NotFound, "Not found"));
    }
}
