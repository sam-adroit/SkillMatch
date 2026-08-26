# SkillMatch

SkillMatch consists of an ASP.NET Core Web API and a React/Vite web application.

## Prerequisites

- .NET 10 SDK
- Node.js 22 or later and npm
- Docker Desktop (optional, for running the backend in a container)

## Project structure

- `SkillMatchBE` — ASP.NET Core Web API
- `SkillMatchFE` — React, TypeScript, and Vite frontend

## Backend — Docker quick start

Run these commands from the repository root:

```powershell
docker build -t skillmatch-be .\SkillMatchBE
Copy-Item .\SkillMatchBE\.env.example .\SkillMatchBE\.env
# Fill in SkillMatchBE/.env before starting the container.
docker run --rm --name skillmatch-be --env-file .\SkillMatchBE\.env -p 5227:8080 skillmatch-be
```

Run the container in the background:

```powershell
docker run -d --rm --name skillmatch-be --env-file .\SkillMatchBE\.env -p 5227:8080 skillmatch-be
```

View logs or stop the container:

```powershell
docker logs -f skillmatch-be
docker stop skillmatch-be
```

Rebuild and restart after backend changes:

```powershell
docker stop skillmatch-be
docker build -t skillmatch-be .\SkillMatchBE
docker run -d --rm --name skillmatch-be --env-file .\SkillMatchBE\.env -p 5227:8080 skillmatch-be
```

## Backend — run without Docker

```powershell
cd .\SkillMatchBE
dotnet restore
dotnet run
```

Build and audit dependencies:

```powershell
dotnet build
dotnet list package --vulnerable --include-transitive
```

### PostgreSQL configuration

The backend uses Entity Framework Core with PostgreSQL. Connection credentials must
come from local .NET User Secrets or Railway variables and must not be added to
`appsettings.json`.

Configure local development from the backend directory:

```powershell
railway connect postgres --tunnel-only --port 61916
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=<PORT>;Database=<DATABASE>;Username=<USER>;Password=<PASSWORD>"
dotnet user-secrets list
```

Keep the Railway tunnel command running in a separate terminal while using the local
database connection.

Remove the local secret when it is no longer needed:

```powershell
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
```

In the Railway backend service, create reference variables pointing to the Postgres
service rather than copying credential values:

```text
PGHOST=${{Postgres.PGHOST}}
PGPORT=${{Postgres.PGPORT}}
PGDATABASE=${{Postgres.PGDATABASE}}
PGUSER=${{Postgres.PGUSER}}
PGPASSWORD=${{Postgres.PGPASSWORD}}
```

If your Railway database service has a different name, replace `Postgres` with its
exact service name. Deploy the staged Railway variable changes before redeploying the
backend.

Verify connectivity after starting the API:

- Local: http://localhost:5227/health/database
- Production: https://api-production-6f48b.up.railway.app/health/database

The endpoint returns HTTP 200 when PostgreSQL is reachable and HTTP 503 otherwise.

For local Docker, keep the database variables in the ignored `SkillMatchBE/.env`
file. Use `host.docker.internal`, not `127.0.0.1`, when PostgreSQL or a Railway
tunnel is running on the Windows host. The container's `127.0.0.1` points back to
the container itself.

Local API documentation:

- Swagger UI: http://localhost:5227/swagger
- OpenAPI JSON: http://localhost:5227/openapi/v1.json

Production API documentation:

- Swagger UI: https://api-production-6f48b.up.railway.app/swagger
- OpenAPI JSON: https://api-production-6f48b.up.railway.app/openapi/v1.json

## Frontend quick start

Create the local environment file once, install dependencies, and start Vite:

```powershell
cd .\SkillMatchFE
Copy-Item .env.example .env
npm install
npm run dev
```

The local frontend is available at http://localhost:5173.

Other frontend commands:

```powershell
npm run lint
npm run build
npm run preview
```

## Environment configuration

The frontend reads the API base URL from `VITE_API_URL`.

Local `SkillMatchFE/.env`:

```env
VITE_API_URL=http://localhost:5227
```

Set this variable in the Railway frontend service for production builds:

```env
VITE_API_URL=https://api-production-6f48b.up.railway.app
```

Local `.env` files are ignored by Git. Commit updates to `.env.example` when the required variables change, but do not commit secrets or local `.env` files.

## Public applications

- Web: https://web-production-6300f7.up.railway.app
- API: https://api-production-6f48b.up.railway.app
