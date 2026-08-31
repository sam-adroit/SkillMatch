# Railway, live-demo, and fallback evidence

Evidence below was collected on 2026-08-29 from the production deployment of commit
`9eb37ddbd9bf51cc3f59733adf9d279aefb9208d`. It contains no credential, JWT,
connection string, private Student text, or OpenAI prompt/response content.

## Public endpoints and expected demo state

- Web: <https://web-production-ff322.up.railway.app>
- API: <https://api-production-84ad.up.railway.app>
- Swagger: <https://api-production-84ad.up.railway.app/swagger>
- Database health: <https://api-production-84ad.up.railway.app/health/database>
- Source: <https://github.com/sam-adroit/SkillMatch>
- Demo users: configured Admin, `demo-student1@skillmatch.local`, and
  `demo-student2@skillmatch.local`; the password remains only in runtime
  configuration. At verification time the dashboard reported 6 Students,
  6 projects, and 1 active team. Counts may increase during a rehearsal.

## Railway deployment evidence

| Service | Deployment | Build/runtime evidence |
|---|---|---|
| API | `addfeddd-c756-424e-9500-7c3172837a93` (`SUCCESS`) | Railway loaded `SKillMatchBE/Dockerfile`, restored/published .NET 10 Release, and produced image digest `sha256:5eb855bf0ea894fc8cf3fb947b609df8a95d23d79b28400d0506d755255c1850`. Runtime is Production, content root `/app`, listening on port 8080. |
| Web | `74d5758d-ae4d-4ef4-bbc0-a1c5253f2a6a` (`SUCCESS`) | Railpack detected a Vite static site, ran npm install plus TypeScript/Vite build, transformed 43 modules, and serves the SPA with Caddy. |
| PostgreSQL | `30cb720e-b601-4545-be6e-4b31a8439e43` (`SUCCESS`) | Private Railway PostgreSQL service is reachable through the API; EF reported the migration history already current. |

The final closed-project correction was deployed from a secret-free source
snapshot on 2026-08-30. API deployment
`266d84e6-c21d-40a1-af53-7a4dcb58b725` reached `SUCCESS` after Railway loaded the
canonical `SKillMatchBE/Dockerfile`; Web deployment
`4901f7c6-5526-4319-9538-e09c5b3f8e0f` also reached `SUCCESS`. The snapshot was
removed after deployment and did not stage or push source changes.

Plan 008 was likewise deployed for pre-approval verification without staging or
pushing. API deployment `d71318d8-40ee-4ecb-91ea-ced208f63acb` (`SUCCESS`) used
`/SKillMatchBE/Dockerfile` and image digest
`sha256:00fcad8743759b9911f5a81c8cc5c2b6f065892c6b998ccdc1d37fd5e67b4f5a`.
Web deployment `74eba142-4682-4792-9682-321a938918e7` (`SUCCESS`) used Railpack's
Vite static-site pipeline and image digest
`sha256:43c49c19d742c5fbfc27a428bc9485a163e9b1d28001c38de038275ed84ea0ea`.
Production migration history includes `20260830200623_AddUserNames`; all 9 users
have required names and an existing account retained its email login.

Production configuration verification:

- `Database__ApplyMigrations=true` with one API replica; no migration remained.
- `DemoSeed__Enabled=false` after the prepared demo records were created.
- Backend `OPENAI_API_KEY`, `OPENAI_MODEL=gpt-5-mini`, and JWT key are configured.
- Web has `VITE_API_URL` only; it has no OpenAI or JWT secret.
- Production values contain no localhost or `host.docker.internal` assumption.
- Runtime logs contained zero high-confidence credential patterns and zero fatal or
  unhandled exceptions in the inspected deployment window.

## HTTPS, CORS, Swagger, and authorization

- HTTPS database health returned 200 with `healthy` / `PostgreSQL`.
- HTTP health redirected to the same HTTPS URL with 301.
- Preflight from the canonical Web origin returned that exact
  `Access-Control-Allow-Origin`; an untrusted origin received no allow-origin header.
- An unauthenticated Student-profile request returned 401.
- An unknown route returned 404 `application/problem+json`.
- Public Swagger returned 29 paths and the global HTTP Bearer scheme. Swagger is
  intentionally public for course demonstration; protected operations still
  require valid role-bearing JWTs.

## Deployed workflow and live OpenAI evidence

The credential-safe smoke script passed health, Swagger, both logins, Student
profile/project/application/team/recommendation reads, Student-to-Admin 403, Admin
dashboard/project/application/team reads, and recommendation generation.

An unchanged Student batch correctly returned three `AiGenerated` results with
`reused: true`. To prove the provider rather than the cache, verification then:

1. loaded `demo-student2` through the authenticated API;
2. temporarily added one harmless preferred-technology marker;
3. generated three recommendations;
4. received `AiGenerated`, `reused: false`, with all explanations nonempty; and
5. restored the original profile in a `finally` block.

The restoration makes the recorded batch stale, so Samuel's next generation after
login must execute the live provider path again. Recommendation history remains the
intended audit trail.

The deployed closed-project boundary was also exercised through a reversible
close/read/reopen probe. My Work's API retained the existing application status,
decision note, and decision timestamp with `projectStatus: Closed`; published
browsing excluded the project and its Student detail route returned 404. The test
restored the selected project to Published in a `finally` block. The remaining UI
label/link behavior is reserved for Samuel's manual approval gate.

## Safe fallback demo

The fallback rehearsal used only task-owned local Docker container
`skillmatch-be-plan007-fallback` with an explicitly empty `OPENAI_API_KEY`; the
Railway key was never edited or retrieved. A harmless demo-profile marker forced a
new recommendation batch and was restored in a `finally` block.

The response contained three results, `providerStatus: Fallback`, `reused: false`,
and three nonempty deterministic explanations. Database health remained
`healthy` / `PostgreSQL`, and authenticated project, application, and team reads all
continued successfully. The container logs contained the expected fallback signal,
zero fatal errors, and zero high-confidence secret patterns. The task-owned
container was stopped/removed and the tunnel closed afterward.

During presentation, show this recorded result and the UI's implemented amber
Fallback notice instead of intentionally breaking production. The production flow
was separately re-proven with fresh `AiGenerated` results.

## Browser presentation captures

`docs/presentation-evidence/home-mobile.png` and `home-desktop.png` capture the
current public Railway landing page at 375x812 and 1440x900. The mobile document's
client and scroll widths were both 360 CSS pixels after scrollbar allocation, so it
had no page-level horizontal overflow. Six visible navigation/actions remained
available and the primary buttons met the 48px touch-height target. A direct mobile
refresh of `/login` also returned the SPA correctly, showed associated Email/Password
labels, 50px inputs, a 48px submit button, equal client/scroll widths, and no browser
console errors. The images contain no authenticated content or secrets.

## Rollback notes

The migrations are additive and the current database is already forward-migrated;
do not drop tables or run destructive schema rollback during a presentation.

For an application regression:

1. preserve logs and identify the last good Git commit/deployment;
2. use `git revert <bad-commit>` on the source branch and push the revert, allowing
   Railway to build both services from reviewable source history;
3. wait for API and Web `SUCCESS`, then check API logs, `/health/database`, Swagger,
   CORS, and both-role smoke tests; and
4. if only a runtime variable changed, restore its prior value in Railway without
   printing it and redeploy that service.

Do not use `railway down`, delete PostgreSQL, expose a public database proxy, or
roll back an additive migration merely to repair an application deployment.
