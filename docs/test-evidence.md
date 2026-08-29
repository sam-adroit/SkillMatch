# Test and deployment evidence

This page records the Plan 006 automated verification performed on 2026-08-29.
The commands are also documented in the root README so they can be repeated from a
fresh checkout. Manual acceptance remains a separate required gate.

## Host verification

| Check | Result | Evidence |
|---|---|---|
| `dotnet restore .\SkillMatchBE\SkillMatchBE.sln` | Pass | Restore completed with all projects current. |
| `dotnet build .\SkillMatchBE\SkillMatchBE.sln --no-restore` | Pass | 0 warnings, 0 errors. |
| `dotnet test .\SkillMatchBE\SkillMatchBE.sln --no-build --no-restore` | Pass | 52 passed, 0 failed, 0 skipped. |
| `npm install` from `SkillMatchFE` | Pass | Dependencies current; audit reported 0 vulnerabilities. |
| `npm run lint --prefix .\SkillMatchFE` | Pass | ESLint completed without errors. |
| `npm run build --prefix .\SkillMatchFE` | Pass | Vite 8.2.0 production build completed; 43 modules transformed. |

The test suite covers controller/API authentication and authorization, service
business rules, repository-backed workflows, recommendation privacy/fallback
behavior, Swagger Bearer configuration, and normal/boundary/error cases for
profiles and lookup maintenance. Normal tests use a fake recommendation provider
and do not spend OpenAI credits.

The first sandboxed test-host attempt could not write the Windows `.NET Runtime`
event log. The identical command passed outside that restricted sandbox; no product
or test change was needed.

## Docker acceptance

The canonical backend artifact was rebuilt cleanly with:

```powershell
docker build --no-cache -t skillmatch-be-plan006 .\SkillMatchBE
```

The image built successfully from the committed multi-stage Dockerfile. It was run
with the same double-underscore and `PG*` environment-variable shape used by
Railway, with secrets supplied only at runtime. Verification through published
port 5230 produced:

- `GET /health/database`: HTTP 200, `healthy`, `PostgreSQL`.
- `GET /swagger/v1/swagger.json`: HTTP 200 with 29 paths and an HTTP `bearer`
  security scheme named `Bearer`.
- Student login/profile/project/application/team/recommendation reads: pass.
- Student request to the Admin dashboard: HTTP 403 as required.
- Admin login/dashboard/project/application/team reads: pass.
- Container logs: Production environment, `/app` content root, listening on
  `http://[::]:8080`, and no pending EF Core migrations.

The first runtime attempt correctly failed closed because an old local Railway
database tunnel was no longer listening after Docker Desktop restarted. Reopening
the tunnel and rerunning the unchanged image passed all checks.

## Railway compatibility

An unexpected GitHub auto-deployment of older remote commit `958f480` temporarily
replaced the previously accepted Plan 005 API/Web runtime. A secret-free snapshot
of the current accepted source was submitted to restore the environment before
Plan 006 verification:

- API deployment `46c991e9-c4cc-4308-ad88-5dc9e090dbdd`: `SUCCESS`.
- Web deployment `8b4e1126-0e34-444d-9be4-cbb8bc17e3ec`: `SUCCESS`.

The restored API returned healthy PostgreSQL status and the current Swagger
recommendation routes. An authenticated production smoke run passed Student and
Admin role boundaries and read workflows. API logs confirmed Production startup
on port 8080 and an up-to-date migration history. No Railway variable or secret was
written to the deployment snapshot or command output.

This restore does not change the approval gate and is not a source commit. Until
the approved local plans are committed and later pushed by the owner, another
automatic deployment from the older remote branch could regress the live demo.

## Documentation and diagram verification

- Every setup command in the root README was exercised in its documented working
  directory. The frontend install instructions were corrected to enter
  `SkillMatchFE`, because npm's `--prefix` option is not an install target.
- `scripts/smoke.ps1` passed both public production checks and authenticated local
  Docker checks without printing credentials or JWTs.
- Seven Mermaid sources rendered successfully to adjacent SVG exports: system
  architecture, domain model, two sequences, communication, and two VOPCs.
- All 16 SRS pages were read and visually inspected. The final implementation's
  deliberate simplifications and requirement coverage are recorded in
  `traceability.md`.
- The secret scan covered tracked and untracked Plan 006 files. Environment files,
  credentials, JWTs, and API keys remain untracked; production demo seeding is
  documented as disabled by default.

No frontend page implementation changed in Plan 006, so the changed-page viewport
inspection requirement is not applicable to this slice. The manual demo checklist
still asks the reviewer to exercise the complete Student and Admin UI at mobile and
desktop widths.
