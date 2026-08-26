using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillMatchBE.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
const string CorsPolicyName = "SkillMatchWeb";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
var databaseConnectionString = GetDatabaseConnectionString(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddDbContextPool<SkillMatchDbContext>(options =>
    options.UseNpgsql(
        databaseConnectionString,
        postgres => postgres.EnableRetryOnFailure()));
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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Restore the original public scheme before HTTPS redirection and OpenAPI generation.
app.UseForwardedHeaders();

// Publish the OpenAPI document and Swagger UI in every environment.
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "SkillMatch API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "SkillMatch API Documentation";
});

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthorization();

app.MapControllers();

app.Run();

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
        IncludeErrorDetail = false
    };

    return connectionStringBuilder.ConnectionString;
}
