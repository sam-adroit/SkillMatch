using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SkillMatchBE.Data;
using SkillMatchBE.Entities;
using SkillMatchBE.Migrations;
using Xunit;

namespace SkillMatchBE.Tests.Unit;

public sealed class UserNameMigrationTests
{
    [Fact]
    public void UserModel_RequiresBoundedNamesAndKeepsEmailUnique()
    {
        var options = new DbContextOptionsBuilder<SkillMatchDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=test;Password=test")
            .Options;
        using var database = new SkillMatchDbContext(options);
        var user = database.Model.FindEntityType(typeof(ApplicationUser))!;

        Assert.False(user.FindProperty(nameof(ApplicationUser.FirstName))!.IsNullable);
        Assert.Equal(100, user.FindProperty(nameof(ApplicationUser.FirstName))!.GetMaxLength());
        Assert.False(user.FindProperty(nameof(ApplicationUser.LastName))!.IsNullable);
        Assert.Equal(100, user.FindProperty(nameof(ApplicationUser.LastName))!.GetMaxLength());
        Assert.Contains(user.GetIndexes(), index =>
            index.IsUnique && index.Properties.Single().Name == nameof(ApplicationUser.NormalizedEmail));
    }

    [Fact]
    public void Migration_DerivesNamesFromEmailBeforeNamesBecomeRequired()
    {
        var operations = new AddUserNames().UpOperations;
        var additions = operations.OfType<AddColumnOperation>().ToArray();
        var backfill = Assert.Single(operations.OfType<SqlOperation>());
        var requiredColumns = operations.OfType<AlterColumnOperation>().ToArray();

        Assert.Equal(2, additions.Length);
        Assert.All(additions, operation => Assert.True(operation.IsNullable));
        Assert.Contains("UPDATE \"Users\"", backfill.Sql);
        Assert.Contains("split_part(\"Email\", '@', 1)", backfill.Sql);
        Assert.Contains("regexp_replace", backfill.Sql);
        Assert.Contains("regexp_split_to_array", backfill.Sql);
        Assert.Contains("INITCAP", backfill.Sql);
        Assert.Contains("LEFT(", backfill.Sql);
        Assert.DoesNotContain("SET \"Email\"", backfill.Sql);
        Assert.Equal(2, requiredColumns.Length);
        Assert.All(requiredColumns, operation => Assert.False(operation.IsNullable));
    }
}
