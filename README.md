# SkillMatch AI

SkillMatch AI is a school-project application for matching students with suitable
projects and helping instructors form balanced teams. The backend is an ASP.NET Core
.NET 10 container using Entity Framework Core and PostgreSQL. The frontend uses
React 19, TypeScript, Vite, and Tailwind CSS.

## Prerequisites

- Docker Desktop, required for the canonical backend workflow
- Railway CLI and access to the linked project, for the PostgreSQL tunnel
- PowerShell 7, for the documented setup and smoke-verification commands
- Node.js 22 or later and npm
- .NET 10 SDK, required for tests and optional direct backend development

## Repository structure

- `SKillMatchBE` — API, controllers, services, repositories, database context, and Dockerfile
- `SkillMatchBE.Tests` — xUnit unit and API integration tests
- `SkillMatchFE` — React, TypeScript, Vite, and Tailwind CSS frontend
- `docs` — Student/Admin guides, demo checklist, traceability, test evidence, and diagrams
- `scripts` — presentation-ready API smoke verification

## Documentation map

- [Student guide](docs/student-guide.md)
- [Admin / instructor guide](docs/admin-guide.md)
- [End-to-end demo checklist](docs/demo-checklist.md)
- [Requirements traceability and simplifications](docs/traceability.md)
- [Test and deployment evidence](docs/test-evidence.md)
- [Presentation checklist](docs/presentation-checklist.md)
- [Railway and fallback evidence](docs/presentation-evidence.md)
- [Architecture, ER/class, sequence, communication, and VOPC diagrams](docs/diagrams/README.md)

## Canonical backend workflow — Docker

The committed `SKillMatchBE/Dockerfile` is the canonical local backend runtime and
the Railway deployment artifact. Run these commands from the repository root.

1. Start the PostgreSQL tunnel in its own terminal:

   ```powershell
   railway connect postgres --tunnel-only --port 61916
   ```

2. Create the ignored container environment file once:

   ```powershell
   Copy-Item .\SKillMatchBE\.env.example .\SKillMatchBE\.env
   ```

   Fill in the tunnel values reported by Railway, generate a unique JWT signing
   key of at least 32 bytes, and choose the local demo Admin password. Keep `PGHOST` set to
   `host.docker.internal`, because a container's `127.0.0.1` points to the
   container itself. `Database__ApplyMigrations=true` applies the committed EF Core
   migrations at startup. `DemoSeed__Enabled=true` creates the configured Admin if
   it does not exist and seeds a small lookup/project catalog plus two complete
   Student profiles (`demo-student1@skillmatch.local` and
   `demo-student2@skillmatch.local`) with presentation-safe full names. The demo Students use the same externally
   configured demo password; no password is hard-coded. Add the backend-only
   `OPENAI_API_KEY`, `OPENAI_MODEL=gpt-5-mini`, and optional
   `OPENAI_TIMEOUT_SECONDS=15` values for live recommendation explanations.
   These values are passed only at container runtime—not as Docker build arguments.
   Keep `DemoSeed__Enabled=false` in production. Never commit `SKillMatchBE/.env`.

3. Build and run the backend image:

   ```powershell
   docker build -t skillmatch-be .\SKillMatchBE
   docker run -d --rm --name skillmatch-be --env-file .\SKillMatchBE\.env -p 5227:8080 skillmatch-be
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
   $student = Invoke-RestMethod -Method Post -Uri http://localhost:5227/api/auth/register -ContentType 'application/json' -Body '{"firstName":"Ada","lastName":"Lovelace","email":"student@example.edu","password":"Choose-A-Student-Password"}'
   Invoke-RestMethod -Uri http://localhost:5227/api/auth/me -Headers @{ Authorization = "Bearer $($student.token)" }
   Invoke-WebRequest -SkipHttpErrorCheck -Uri http://localhost:5227/api/admin/auth-check -Headers @{ Authorization = "Bearer $($student.token)" }
   ```

   Registration requires first name, last name, email, and password and must return a
   `Student`; `/api/auth/me` must return that Student and their names;
   the Admin endpoint must return HTTP 403 for the Student token. Login is
   `POST /api/auth/login` with the same email/password JSON shape.

6. Authenticate protected endpoints in Swagger:

   1. Open <http://localhost:5227/swagger>.
   2. Run `POST /api/auth/login` with the seeded Admin email and password from
      the ignored `SKillMatchBE/.env` file.
   3. Copy only the `token` field from the successful response.
   4. Click **Authorize** in Swagger and paste the token exactly as the dialog
      instructs. Do not add a `Bearer ` prefix when the dialog requests only the token.
   5. Run a protected endpoint such as `GET /api/skills` and confirm HTTP 200.
   6. Run an Admin endpoint such as `GET /api/admin/projects` and confirm HTTP 200.
      Student-only `GET /api/profile` should return HTTP 403 with the Admin token;
      use a Student login token when testing that endpoint.

7. View logs or stop the task-created container:

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
cd .\SKillMatchBE
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=61916;Database=<DATABASE>;Username=<USER>;Password=<PASSWORD>"
dotnet user-secrets set "Jwt:Issuer" "SkillMatchBE"
dotnet user-secrets set "Jwt:Audience" "SkillMatchFE"
dotnet user-secrets set "Jwt:Key" "<GENERATE-A-RANDOM-SECRET-OF-AT-LEAST-32-BYTES>"
dotnet user-secrets set "Database:ApplyMigrations" "true"
dotnet user-secrets set "OPENAI_API_KEY" "<OPENAI-PROJECT-KEY>"
dotnet user-secrets set "OPENAI_MODEL" "gpt-5-mini"
dotnet run --launch-profile http
```

Remove the local secret when it is no longer required:

```powershell
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
dotnet user-secrets remove "Jwt:Key"
dotnet user-secrets remove "OPENAI_API_KEY"
```

## Frontend workflow

From the repository root, create the local environment file once, install
dependencies, and start Vite:

```powershell
Copy-Item .\SkillMatchFE\.env.example .\SkillMatchFE\.env
Push-Location .\SkillMatchFE
npm install
npm run dev
Pop-Location
```

The frontend is available at <http://localhost:5173> and calls the backend URL in
`VITE_API_URL`. Frontend variables are public build configuration and must not
contain secrets.

## Automated verification

Run these checks from the repository root:

```powershell
dotnet restore .\SKillMatchBE\SkillMatchBE.sln
dotnet build .\SKillMatchBE\SkillMatchBE.sln
dotnet test .\SKillMatchBE\SkillMatchBE.sln
npm run lint --prefix .\SkillMatchFE
npm run build --prefix .\SkillMatchFE
```

Normal tests mock the recommendation provider and spend no API credits. Run the
explicit live Responses API smoke test only when intended; it loads the key from
the backend's existing user-secrets without printing it:

```powershell
$env:RUN_OPENAI_SMOKE_TEST = "1"
dotnet test .\SKillMatchBE\SkillMatchBE.sln --filter "Category=OpenAISmoke"
Remove-Item Env:RUN_OPENAI_SMOKE_TEST
```

The API integration tests use an isolated, non-connecting PostgreSQL connection
string. A successful host build/test does not replace the Docker verification above.

After the Docker container is healthy, run the compact read-only smoke script. The
credentials are prompted securely and are not printed. Omit either credential to
skip that role's checks. Add `-GenerateRecommendation` only when a live OpenAI call
or stored-batch reuse is intentionally part of the check.

```powershell
$studentCredential = Get-Credential -Message "Demo Student" -UserName "demo-student1@skillmatch.local"
$adminCredential = Get-Credential -Message "Demo Admin" -UserName "admin@skillmatch.local"
.\scripts\smoke.ps1 -BaseUrl http://localhost:5227 -StudentCredential $studentCredential -AdminCredential $adminCredential
```

For public infrastructure-only verification without credentials:

```powershell
.\scripts\smoke.ps1 -BaseUrl https://api-production-84ad.up.railway.app
```

Create a new migration after changing the EF Core model with the repository-local tool:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> --project .\SKillMatchBE --startup-project .\SKillMatchBE --output-dir Migrations
```

## API behavior

- Swagger UI: <http://localhost:5227/swagger>
- Swagger JSON: <http://localhost:5227/swagger/v1/swagger.json>
- PostgreSQL health: <http://localhost:5227/health/database>
- Register: `POST /api/auth/register`
- Login: `POST /api/auth/login`
- Current user: `GET /api/auth/me` (bearer token required)
- Admin authorization check: `GET /api/admin/auth-check` (Admin bearer token required)
- Student profile: `GET/PUT /api/profile` (Student bearer token required)
- Lookup catalogs: `GET /api/skills`, `/api/interests`, and `/api/categories`
- Published project catalog/detail: `GET /api/projects` and `GET /api/projects/{id}`
- Admin lookup CRUD: `POST /api/admin/{skills|interests|categories}` plus `PUT/DELETE` with an ID
- Admin project management: `GET/POST /api/admin/projects`, `PUT/DELETE /api/admin/projects/{id}`, and `PATCH /api/admin/projects/{id}/status`
- Student applications: `POST /api/projects/{id}/applications` and `GET /api/applications`; responses include the project's current status so closed-project history remains visible without a dead detail link
- Admin application review: `GET /api/admin/applications` with optional `status`/`projectId` filters and `PATCH /api/admin/applications/{id}/decision`
- Team views: `GET /api/teams` and `GET /api/teams/{id}`; Students receive only their active teams
- Admin team management: `POST /api/admin/teams` and `PUT /api/admin/teams/{id}`
- Admin counts: `GET /api/admin/dashboard`
- Ranked project recommendations: `POST /api/recommendations/projects` (Student)
- Recommendation history: `GET /api/recommendations/history` (Student)
- Available teammate suggestions: `GET /api/recommendations/teammates` (Student)
- Team skill gaps: `GET /api/teams/{id}/skill-gaps` (Admin or that team's Student members)

The health endpoint returns HTTP 200 when PostgreSQL is reachable and HTTP 503
otherwise. Unknown routes and unhandled API errors use Problem Details JSON with a
trace ID. Public registration always creates a Student account and requires bounded
first and last names. Email remains the normalized unique login credential. Passwords are stored
with ASP.NET Core Identity-compatible hashing; JWTs expire and carry the server-side
Student/Admin role used by authorization policies.

Project search is evaluated in PostgreSQL and supports `search`, `skillId`,
`categoryId`, `difficulty`, `available`, and `teamSize` query parameters. Students
only receive published projects; unpublished and closed projects return 404 from
the student detail endpoint. My Work preserves existing applications and decisions
for a closed project, labels it **Project closed**, and does not show a working-looking
detail link. Admin notes are present only in Admin project responses.
Ordinary application, team, member/leader selection, profile, dashboard, and
navigation labels use full names rather than email addresses. Intentionally opaque
teammate recommendations remain anonymous. User-triggered mutations report
closable success/error toasts; field validation and page-load failures remain inline.
Project titles and lookup names are unique without regard to case. Projects require
at least one skill and team sizes ordered as minimum ≤ preferred ≤ maximum. Only
Draft projects can be deleted; publish or close projects through the status endpoint.

The `AddProfilesAndProjects` migration is additive: it creates normalized lookup,
student-profile, project, and join tables without rewriting existing users. Startup
migration execution remains suitable for the current single API instance. Run
migrations as a separate deployment step before scaling the API horizontally.

The additive `AddApplicationsAndTeams` migration creates applications, teams, and
membership tables with unique Student/project applications, one team per project,
status query indexes, decision/team timestamps, and restrictive foreign keys.
Application decisions and membership changes run in serializable PostgreSQL
transactions. A Student needs a saved profile to apply; projects must be Published;
duplicate applications, approvals beyond project capacity, unapproved team members,
and a second active team assignment in the implicit course cycle are rejected.

The additive `AddRecommendations` migration stores project recommendation score,
explanation, provider, model, AI/fallback status, and timestamp history. Project
ranking uses a documented 100-point deterministic score: required-skill overlap
50, interest/category alignment 20, preferred-technology alignment 15, and
difficulty/experience fit 15. Ties are stable by project title and ID. Up to three
published projects are sent in one compact OpenAI Responses API request for short
structured explanations. The payload excludes identity, email, goals, passwords,
application notes, and other private data. Current results are reused only until
the Student profile, ranked project data, or target set changes.

If OpenAI configuration is missing, the request times out, or the provider returns
an error or malformed response, the API stores and returns a deterministic
`Fallback` explanation. The UI labels fallback mode clearly, while profile,
project, application, and team workflows continue normally. A fallback result is
not evidence for Plan 005 acceptance; the deployed UI must show `AI generated`.

Teammate suggestions exclude the requesting Student, inactive accounts, Students
without profiles, and anyone assigned to an active team. Responses expose only an
opaque Student label and shared/complementary skill and interest facts—never email,
goals, credentials, or application notes. Team skill gaps are the project-required
skills minus the union of current member skills.

## Recommendation demo flow

1. As a demo Student with a complete profile, open **Recommendations** and choose
   **Generate recommendations**.
2. Confirm ranked score, matched skills, growth areas, and an **AI generated** badge;
   refresh/generate again to see the unchanged batch reused in history.
3. Edit a profile skill or preferred technology, save, and generate again. Confirm
   a new stored batch and sensible ranking or explanation changes.
4. Review available teammate suggestions; confirm no email or private profile text
   is displayed and assigned Students are absent.
5. Open **My work** as a team member or the Admin workflow page to review required
   skill coverage. Add an approved member with a missing skill and confirm the gap
   disappears after the team refreshes.

For a safe fallback check, omit or replace `OPENAI_API_KEY` only in a disposable
local container, generate recommendations, and confirm the visible fallback notice.
Restore the real key before live-AI acceptance. Never alter the production key to
test an outage.

## Application and team demo flow

1. As Admin, publish a project and note its maximum team size.
2. As a demo Student, complete the profile if needed, open the project, submit an
   application, and verify Pending status under **My work**.
3. As Admin, open **Applications, teams, and dashboard**, filter Pending
   applications, and mark the application Approved.
4. Select the project in the team form, select only Approved Students, choose one
   selected member as leader, and create the team.
5. Return as the Student and verify Approved status plus team name, leader, and
   membership under **My work**. Dashboard counts should update after each action.

## Railway configuration

Railway builds the API from `SKillMatchBE/Dockerfile`. The backend supports the same
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
OPENAI_API_KEY=<RAILWAY-BACKEND-ONLY-PROJECT-KEY>
OPENAI_MODEL=gpt-5-mini
OPENAI_TIMEOUT_SECONDS=15
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
Set OpenAI variables only on Railway's backend `API` service; never add them to the
frontend or use a `VITE_` prefix. The backend calls
<https://api.openai.com/v1/responses>; the browser calls only the SkillMatch API.

Production endpoints:

- Web: <https://web-production-ff322.up.railway.app>
- API: <https://api-production-84ad.up.railway.app>
- Swagger: <https://api-production-84ad.up.railway.app/swagger>
- PostgreSQL health: <https://api-production-84ad.up.railway.app/health/database>

Railway detects the Dockerfile's production listener on port 8080 and routes its
public HTTPS domain to that container port. The same double-underscore configuration
names work unchanged in local Docker and Railway. The API remains a single-instance
deployment for startup migration execution; migrate separately before scaling beyond
one instance.

The `AddUserNames` startup migration safely handles existing Railway accounts by
adding nullable name columns, deriving display names from the email local part, and
then making the columns required. For example, `jane.doe@example.edu` becomes
`Jane Doe`; a local part with no separator uses `User` as the last name. The
migration does not modify email, password hashes, roles, or relationships, so email
remains the unique login credential.

## Troubleshooting

| Symptom | Check and resolution |
|---|---|
| Container cannot reach PostgreSQL | Keep the Railway tunnel open, copy its port into `PGPORT`, and use `PGHOST=host.docker.internal` from Docker. |
| `/health/database` returns 503 | Verify the five `PG*` values, tunnel, network access, and PostgreSQL service status before changing code. |
| API exits during startup | Confirm `Jwt__Key` is at least 32 UTF-8 bytes and enabled demo seeding has a valid Admin email/password of at least 12 characters. |
| Browser reports a CORS error | Confirm `VITE_API_URL` names the API, and the browser origin appears in backend `Cors__AllowedOrigins`. Never put an OpenAI key in Vite configuration. |
| Swagger returns 401/403 | Log in again, use **Authorize** with only the token, and confirm the selected endpoint permits the token's Student/Admin role. |
| Recommendation shows Fallback | Check backend-only OpenAI variables, billing, outbound HTTPS, provider timeout, and API logs. Ordinary workflows should remain available. |
| Recommendation does not call OpenAI again | An unchanged current AI batch is intentionally reused. Save a profile skill/technology or update a ranked project to invalidate it. |
| Migration fails in Railway | Keep one API instance for startup migration, inspect deployment logs, and verify the database user can apply the additive migrations. |
| Frontend route returns 404 after refresh | Confirm Railway detected the Vite static-site build and Caddy SPA fallback; redeploy the Web service from `SkillMatchFE`. |

For a clean rehearsal, stop/remove only your task-created container, reopen the
PostgreSQL tunnel, recreate ignored `.env` files from their examples, and repeat the
canonical Docker, frontend, automated verification, and smoke commands in order.
