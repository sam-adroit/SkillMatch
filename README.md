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
docker run --rm --name skillmatch-be -p 5227:8080 skillmatch-be
```

Run the container in the background:

```powershell
docker run -d --rm --name skillmatch-be -p 5227:8080 skillmatch-be
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
docker run -d --rm --name skillmatch-be -p 5227:8080 skillmatch-be
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
