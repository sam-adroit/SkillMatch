# SkillMatch backend

The backend is an ASP.NET Core .NET 10 REST API with EF Core, PostgreSQL, JWT bearer
authentication, and a backend-only OpenAI Responses adapter. The canonical runtime
is the multi-stage `Dockerfile`; direct `dotnet run` is development-only.

## Architecture

Requests follow controller -> service -> focused repository -> `SkillMatchDbContext`.
DTOs define HTTP contracts, services enforce role/ownership/business rules, and EF
Core repositories perform persistence. Recommendation scoring is deterministic;
OpenAI supplies privacy-minimized top-three explanations with a visible fallback.
Application responses include the project's current status so clients can retain
closed-project history without linking Students to a detail endpoint that correctly
returns 404.
Users have required bounded first/last names while email remains the unique login
credential. Application and team DTOs expose full display names instead of email;
the intentionally anonymous teammate recommendation contract is unchanged.

See the root [README](../README.md) for the exercised Docker/Railway setup and
[architecture diagrams](../docs/diagrams/README.md) for the final topology and model.

## Verification

From the repository root:

```powershell
dotnet restore .\SKillMatchBE\SkillMatchBE.sln
dotnet build .\SKillMatchBE\SkillMatchBE.sln
dotnet test .\SKillMatchBE\SkillMatchBE.sln
docker build -t skillmatch-be .\SKillMatchBE
```

Normal tests mock OpenAI. The explicitly gated live smoke test is documented in the
root README and uses existing user-secrets without printing the key.

## Runtime configuration

Use either `ConnectionStrings__DefaultConnection` or Railway-shaped `PGHOST`,
`PGPORT`, `PGDATABASE`, `PGUSER`, and `PGPASSWORD`. Other backend-only settings are
`Jwt__*`, `Database__ApplyMigrations`, `DemoSeed__*`, `OPENAI_API_KEY`,
`OPENAI_MODEL`, and `OPENAI_TIMEOUT_SECONDS`. Never pass secrets as Docker build
arguments or expose them through `VITE_` variables.

Swagger is `/swagger`; database health is `/health/database`. Startup migrations are
documented for the current single-instance school deployment and must become a
separate deployment step before horizontally scaling the API.

`AddUserNames` is additive: it creates nullable name columns, backfills every
existing row from the email local part, and only then makes both columns required.
Non-alphanumeric separators become name boundaries (`jane.doe@example.edu` becomes
`Jane Doe`); a one-part local name receives `User` as its last name. Values are
bounded to the same 100-character column limits. Existing password hashes,
normalized email login identifiers, roles, and relationships are not rewritten.
