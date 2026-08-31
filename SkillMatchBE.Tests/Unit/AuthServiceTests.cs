using Microsoft.AspNetCore.Identity;
using SkillMatchBE.Auth;
using SkillMatchBE.DTOs.Auth;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_CreatesStudentWithHashedPasswordAndNormalizedEmail()
    {
        var repository = new MemoryUserRepository();
        var service = CreateService(repository);

        var result = await service.RegisterAsync(
            new RegisterRequest(" Ada ", " Lovelace ", " Student@Example.edu ", "correct-horse-battery"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.Added);
        Assert.Equal(UserRole.Student, repository.Added.Role);
        Assert.Equal("Ada", repository.Added.FirstName);
        Assert.Equal("Lovelace", repository.Added.LastName);
        Assert.Equal("STUDENT@EXAMPLE.EDU", repository.Added.NormalizedEmail);
        Assert.NotEqual("correct-horse-battery", repository.Added.PasswordHash);
        Assert.Equal("Student", result.Response?.User.Role);
        Assert.Equal("Ada", result.Response?.User.FirstName);
        Assert.Equal("Lovelace", result.Response?.User.LastName);
    }

    [Fact]
    public async Task Register_DuplicateEmailDoesNotCreateAnotherUser()
    {
        var repository = new MemoryUserRepository
        {
            Existing = CreateUser("student@example.edu", UserRole.Student)
        };
        var service = CreateService(repository);

        var result = await service.RegisterAsync(
            new RegisterRequest("Ada", "Lovelace", "STUDENT@example.edu", "correct-horse-battery"),
            CancellationToken.None);

        Assert.Equal(AuthFailure.DuplicateEmail, result.Failure);
        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task Login_WithWrongPasswordFailsWithoutIssuingToken()
    {
        var user = CreateUser("student@example.edu", UserRole.Student);
        var hasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, "right-password");
        var repository = new MemoryUserRepository { Existing = user };
        var tokenService = new StubTokenService();
        var service = new AuthService(repository, hasher, tokenService, new TestClock());

        var result = await service.LoginAsync(
            new LoginRequest(user.Email, "wrong-password"),
            CancellationToken.None);

        Assert.Equal(AuthFailure.InvalidCredentials, result.Failure);
        Assert.False(tokenService.WasCalled);
    }

    private static AuthService CreateService(MemoryUserRepository repository) =>
        new(
            repository,
            new PasswordHasher<ApplicationUser>(),
            new StubTokenService(),
            new TestClock());

    private static ApplicationUser CreateUser(string email, UserRole role) => new()
    {
        FirstName = "Existing",
        LastName = "Student",
        Email = email,
        NormalizedEmail = AuthService.NormalizeEmail(email),
        PasswordHash = "placeholder",
        Role = role,
        CreatedAt = DateTimeOffset.Parse("2026-08-27T00:00:00Z")
    };

    private sealed class MemoryUserRepository : IUserRepository
    {
        public ApplicationUser? Existing { get; init; }
        public ApplicationUser? Added { get; private set; }

        public Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Existing?.Id == id ? Existing : null);

        public Task<ApplicationUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(Existing?.NormalizedEmail == normalizedEmail ? Existing : null);

        public Task<bool> TryAddAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            Added = user;
            return Task.FromResult(true);
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class StubTokenService : IJwtTokenService
    {
        public bool WasCalled { get; private set; }

        public IssuedToken CreateToken(ApplicationUser user)
        {
            WasCalled = true;
            return new IssuedToken("test-token", DateTimeOffset.Parse("2026-08-27T01:00:00Z"));
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-08-27T00:00:00Z");
    }
}
