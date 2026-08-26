using Microsoft.EntityFrameworkCore;

namespace SkillMatchBE.Data;

public sealed class SkillMatchDbContext(DbContextOptions<SkillMatchDbContext> options)
    : DbContext(options);
