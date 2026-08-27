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

   Fill in the tunnel values reported by Railway. Keep `PGHOST` set to
   `host.docker.internal`, because a container's `127.0.0.1` points to the
   container itself. Never commit `SkillMatchBE/.env`.

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

5. View logs or stop the task-created container:

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
dotnet run --launch-profile http
```

Remove the local secret when it is no longer required:

```powershell
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
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

## API behavior

- Swagger UI: <http://localhost:5227/swagger>
- Swagger JSON: <http://localhost:5227/swagger/v1/swagger.json>
- PostgreSQL health: <http://localhost:5227/health/database>

The health endpoint returns HTTP 200 when PostgreSQL is reachable and HTTP 503
otherwise. Unknown routes and unhandled API errors use Problem Details JSON with a
trace ID.

## Railway configuration

Railway builds the API from `SkillMatchBE/Dockerfile`. The backend supports the same
environment-variable shape used by the local container:

```text
PGHOST=${{Postgres.PGHOST}}
PGPORT=${{Postgres.PGPORT}}
PGDATABASE=${{Postgres.PGDATABASE}}
PGUSER=${{Postgres.PGUSER}}
PGPASSWORD=${{Postgres.PGPASSWORD}}
```

If the database service has a different name, replace `Postgres` with its exact
Railway service name. Railway supplies its internal PostgreSQL host directly; only
the local Docker/tunnel workflow substitutes `host.docker.internal`.

The frontend remains a direct Railway deployment and uses this public build variable:

```env
VITE_API_URL=https://api-production-84ad.up.railway.app
```

Production endpoints:

- Web: <https://web-production-ff322.up.railway.app>
- API: <https://api-production-84ad.up.railway.app>
- Swagger: <https://api-production-84ad.up.railway.app/swagger>
- PostgreSQL health: <https://api-production-84ad.up.railway.app/health/database>

No demo seeding or test-only configuration is enabled in this baseline or in the
production settings.
