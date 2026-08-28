# SkillMatch AI

SkillMatch AI is a school-project application for matching students with suitable
projects and helping instructors form balanced teams. The backend is an ASP.NET Core
.NET 10 container using Entity Framework Core and PostgreSQL. The frontend uses
React 19, TypeScript, Vite, and Tailwind CSS.

## Prerequisites

- Docker Desktop, required for the canonical backend workflow
- Railway CLI and access to the linked project, for the PostgreSQL tunnel
- Node.js 22 or later and npm
- .NET 10 SDK, required for tests and optional direct backend development

## Repository structure

- `SkillMatchBE` — API, database context, Dockerfile, and planned application layers
- `SkillMatchBE.Tests` — xUnit unit and API integration tests
- `SkillMatchFE` — React, TypeScript, Vite, and Tailwind CSS frontend

## Canonical backend workflow — Docker

The committed `SkillMatchBE/Dockerfile` is the canonical local backend runtime and
the Railway deployment artifact. Run these commands from the repository root.

1. Start the PostgreSQL tunnel in its own terminal:

   ```powershell
   railway connect postgres --tunnel-only --port 61916
   ```

2. Create the ignored container environment file once:

   ```powershell
   Copy-Item .\SkillMatchBE\.env.example .\SkillMatchBE\.env
   ```

   Fill in the tunnel values reported by Railway, generate a unique JWT signing
   key of at least 32 bytes, and choose the local demo Admin password. Keep `PGHOST` set to
   `host.docker.internal`, because a container's `127.0.0.1` points to the
   container itself. `Database__ApplyMigrations=true` applies the committed EF Core
   migrations at startup. `DemoSeed__Enabled=true` creates the configured Admin if
   it does not exist. Never commit `SkillMatchBE/.env`.

3. Build and run the backend image:

   ```powershell
   docker build -t skillmatch-be .\SkillMatchBE
   docker run -d --rm --name skillmatch-be --env-file .\SkillMatchBE\.env -p 5227:8080 skillmatch-be
   ```

4. Verify the containerized API:

   ```powershell
   Invoke-WebRequest http://localhost:5227/swagger/v1/swagger.json -UseBasicParsing
   Invoke-RestMethod http://localhost:5227/health/database
   ```

   Swagger must return HTTP 200. The health response must contain
   `status: healthy` and `database: PostgreSQL`.

5. Exercise authentication through the container (replace the example credentials):

   ```powershell
   $student = Invoke-RestMethod -Method Post -Uri http://localhost:5227/api/auth/register -ContentType 'application/json' -Body '{"email":"student@example.edu","password":"Choose-A-Student-Password"}'
   Invoke-RestMethod -Uri http://localhost:5227/api/auth/me -Headers @{ Authorization = "Bearer $($student.token)" }
   Invoke-WebRequest -SkipHttpErrorCheck -Uri http://localhost:5227/api/admin/auth-check -Headers @{ Authorization = "Bearer $($student.token)" }
   ```

   Registration must return a `Student`; `/api/auth/me` must return that Student;
   the Admin endpoint must return HTTP 403 for the Student token. Login is
   `POST /api/auth/login` with the same email/password JSON shape.

6. View logs or stop the task-created container:

   ```powershell
   docker logs skillmatch-be
   docker stop skillmatch-be
   ```

The container listens on port 8080 internally and is published at
<http://localhost:5227>. Local Swagger is at <http://localhost:5227/swagger>.

## Optional backend workflow — direct .NET

Direct `dotnet run` is useful for fast development but does not satisfy backend
acceptance. Configure the connection string through .NET User Secrets and point it
at the host tunnel:

```powershell
cd .\SkillMatchBE
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=61916;Database=<DATABASE>;Username=<USER>;Password=<PASSWORD>"
dotnet user-secrets set "Jwt:Issuer" "SkillMatchBE"
dotnet user-secrets set "Jwt:Audience" "SkillMatchFE"
dotnet user-secrets set "Jwt:Key" "<GENERATE-A-RANDOM-SECRET-OF-AT-LEAST-32-BYTES>"
dotnet user-secrets set "Database:ApplyMigrations" "true"
dotnet run --launch-profile http
```

Remove the local secret when it is no longer required:

```powershell
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
dotnet user-secrets remove "Jwt:Key"
```

## Frontend workflow

From the repository root, create the local environment file once, install
dependencies, and start Vite:

```powershell
Copy-Item .\SkillMatchFE\.env.example .\SkillMatchFE\.env
npm install --prefix .\SkillMatchFE
npm run dev --prefix .\SkillMatchFE
```

The frontend is available at <http://localhost:5173> and calls the backend URL in
`VITE_API_URL`. Frontend variables are public build configuration and must not
contain secrets.

## Automated verification

Run these checks from the repository root:

```powershell
dotnet restore .\SkillMatchBE\SkillMatchBE.sln
dotnet build .\SkillMatchBE\SkillMatchBE.sln
dotnet test .\SkillMatchBE\SkillMatchBE.sln
npm run lint --prefix .\SkillMatchFE
npm run build --prefix .\SkillMatchFE
```

The API integration tests use an isolated, non-connecting PostgreSQL connection
string. A successful host build/test does not replace the Docker verification above.

Create a new migration after changing the EF Core model with the repository-local tool:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> --project .\SkillMatchBE --startup-project .\SkillMatchBE --output-dir Migrations
```

## API behavior

- Swagger UI: <http://localhost:5227/swagger>
- Swagger JSON: <http://localhost:5227/swagger/v1/swagger.json>
- PostgreSQL health: <http://localhost:5227/health/database>
- Register: `POST /api/auth/register`
- Login: `POST /api/auth/login`
- Current user: `GET /api/auth/me` (bearer token required)
- Admin authorization check: `GET /api/admin/auth-check` (Admin bearer token required)

The health endpoint returns HTTP 200 when PostgreSQL is reachable and HTTP 503
otherwise. Unknown routes and unhandled API errors use Problem Details JSON with a
trace ID. Public registration always creates a Student account. Passwords are stored
with ASP.NET Core Identity-compatible hashing; JWTs expire and carry the server-side
Student/Admin role used by authorization policies.

## Railway configuration

Railway builds the API from `SkillMatchBE/Dockerfile`. The backend supports the same
environment-variable shape used by the local container:

```text
PGHOST=${{Postgres.PGHOST}}
PGPORT=${{Postgres.PGPORT}}
PGDATABASE=${{Postgres.PGDATABASE}}
PGUSER=${{Postgres.PGUSER}}
PGPASSWORD=${{Postgres.PGPASSWORD}}
Database__ApplyMigrations=true
Jwt__Issuer=SkillMatchBE
Jwt__Audience=SkillMatchFE
Jwt__Key=<RAILWAY-GENERATED-SECRET-OF-AT-LEAST-32-BYTES>
Jwt__ExpiresMinutes=60
DemoSeed__Enabled=false
```

If the database service has a different name, replace `Postgres` with its exact
Railway service name. Railway supplies its internal PostgreSQL host directly; only
the local Docker/tunnel workflow substitutes `host.docker.internal`.

The frontend remains a direct Railway deployment and uses this public build variable:

```env
VITE_API_URL=https://api-production-84ad.up.railway.app
```

To expose a safe demo Admin, set `DemoSeed__Enabled=true` plus
`DemoSeed__AdminEmail` and `DemoSeed__AdminPassword` as Railway variables. Keep the
password only in Railway/runtime configuration, use at least 12 characters, and
disable the seed outside the demo environment. No credential is committed.

Production endpoints:

- Web: <https://web-production-ff322.up.railway.app>
- API: <https://api-production-84ad.up.railway.app>
- Swagger: <https://api-production-84ad.up.railway.app/swagger>
- PostgreSQL health: <https://api-production-84ad.up.railway.app/health/database>

The Dockerfile accepts Railway's dynamic `PORT`, while the same double-underscore
configuration names work unchanged in local Docker and Railway. The API remains a
single-instance deployment for startup migration execution; migrate separately
before scaling beyond one instance.
