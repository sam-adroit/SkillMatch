using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Data;

public sealed class SkillMatchDbContext(DbContextOptions<SkillMatchDbContext> options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<ApplicationUser>();

        user.ToTable("Users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Email).HasMaxLength(254).IsRequired();
        user.Property(item => item.NormalizedEmail).HasMaxLength(254).IsRequired();
        user.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(item => item.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        user.HasIndex(item => item.NormalizedEmail).IsUnique();
    }
}
