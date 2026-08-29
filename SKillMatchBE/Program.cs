using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using SkillMatchBE.Auth;
using SkillMatchBE.Data;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Recommendations;
using SkillMatchBE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
const string CorsPolicyName = "SkillMatchWeb";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
var databaseConnectionString = GetDatabaseConnectionString(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddDbContextPool<SkillMatchDbContext>(options =>
    options.UseNpgsql(
        databaseConnectionString,
        postgres => postgres.EnableRetryOnFailure()));
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Encoding.UTF8.GetByteCount(options.Key) >= 32,
        "Jwt:Key must contain at least 32 UTF-8 bytes.")
    .ValidateOnStart();
builder.Services
    .AddOptions<DemoSeedOptions>()
    .Bind(builder.Configuration.GetSection(DemoSeedOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !options.Enabled ||
            (new EmailAddressAttribute().IsValid(options.AdminEmail) &&
             options.AdminPassword.Length >= 12),
        "Enabled demo seeding requires an Admin email and password of at least 12 characters.")
    .ValidateOnStart();
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services
    .AddOptions<OpenAIOptions>()
    .Configure(options =>
    {
        options.ApiKey = builder.Configuration["OPENAI_API_KEY"] ?? builder.Configuration["OpenAI:ApiKey"] ?? string.Empty;
        options.Model = builder.Configuration["OPENAI_MODEL"] ?? builder.Configuration["OpenAI:Model"] ?? "gpt-5-mini";
        options.TimeoutSeconds = int.TryParse(builder.Configuration["OPENAI_TIMEOUT_SECONDS"], out var timeout) ? timeout : 15;
    })
    .ValidateDataAnnotations();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Email,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IApplicationTeamRepository, ApplicationTeamRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddHttpClient<IRecommendationProvider, OpenAIRecommendationProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
});
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Railway assigns dynamic proxy addresses, so there is no stable proxy IP to list.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the JWT token returned by POST /api/auth/login. Do not add the Bearer prefix."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Restore the original public scheme before HTTPS redirection and OpenAPI generation.
app.UseForwardedHeaders();

// Publish Swagger documentation in every environment.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillMatch API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "SkillMatch API Documentation";
});

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await InitializeDatabaseAsync(app);

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    var databaseOptions = app.Configuration
        .GetSection(DatabaseOptions.SectionName)
        .Get<DatabaseOptions>() ?? new DatabaseOptions();
    var demoOptions = app.Configuration
        .GetSection(DemoSeedOptions.SectionName)
        .Get<DemoSeedOptions>() ?? new DemoSeedOptions();

    if (!databaseOptions.ApplyMigrations && !demoOptions.Enabled)
    {
        return;
    }

    await using var scope = app.Services.CreateAsyncScope();

    if (databaseOptions.ApplyMigrations)
    {
        var database = scope.ServiceProvider.GetRequiredService<SkillMatchDbContext>();
        await database.Database.MigrateAsync();
    }

    if (demoOptions.Enabled)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}

static string GetDatabaseConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    var host = configuration["PGHOST"];
    var database = configuration["PGDATABASE"];
    var username = configuration["PGUSER"];
    var password = configuration["PGPASSWORD"];

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "Database configuration is missing. Configure ConnectionStrings:DefaultConnection " +
            "or the Railway PGHOST, PGPORT, PGDATABASE, PGUSER, and PGPASSWORD variables.");
    }

    var connectionStringBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = int.TryParse(configuration["PGPORT"], out var port) ? port : 5432,
        Database = database,
        Username = username,
        Password = password,
        ApplicationName = "SkillMatchBE",
        GssEncryptionMode = GssEncryptionMode.Disable,
        IncludeErrorDetail = false
    };

    return connectionStringBuilder.ConnectionString;
}

public partial class Program;
