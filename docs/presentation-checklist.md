# SkillMatch AI presentation checklist

This is an 8-10 minute rubric-oriented rehearsal. Keep the public Web application
open in a private browser window, keep credentials outside the projected screen,
and use the prepared demo records instead of typing secrets or lengthy content.

## Before presenting

- Open the public Web application, API health endpoint, GitHub repository, diagram
  index, traceability matrix, and test evidence in separate tabs.
- Confirm Railway shows `SUCCESS` for API, Web, and PostgreSQL and that database
  health says `healthy` / `PostgreSQL`.
- Log in once as `demo-student2@skillmatch.local` and once as the configured Admin;
  obtain the shared demo password from the secure runtime configuration.
- Confirm the Student profile is complete, at least one project is Published, the
  live OpenAI result is not currently in Fallback mode, and browser zoom is 100%.
- Silence notifications. Never project Railway variables, `.env`, user-secrets,
  JWTs, login responses, or the OpenAI dashboard.

## Timed path

| Time | Rubric evidence and action |
|---|---|
| 0:00-0:45 | **Problem and vision:** Students need structured project discovery and instructors need one place for applications and balanced teams. State the two roles and the advisory role of AI. |
| 0:45-1:30 | **SRS and use cases:** Open `traceability.md`; point to authentication, profiles, project/application/team workflows, recommendations, teammate privacy, skill gaps, and the documented simplifications. |
| 1:30-2:30 | **Architecture and modeling:** Show system architecture, domain model, apply/recommendation sequences, communication diagram, and both VOPCs. Explain React -> controller -> service -> repository -> EF/PostgreSQL, with the recommendation provider beside deterministic scoring. |
| 2:30-3:15 | **Code quality:** Open one controller, `RecommendationService`, a focused repository, entity/DbContext mapping, and an xUnit test. Emphasize DTO-only APIs, dependency injection, async calls, transactions, named scoring weights, and readable boundary tests. |
| 3:15-4:10 | **Admin UI:** Log in as Admin, show dashboard counts, open the project form, and identify category, difficulty, required skills, capacity, and publish/close controls. Show application decisions and team membership/leader controls. |
| 4:10-5:00 | **Student UI:** Log in as Student, show the complete profile, filter Published projects, open a detail page, and note that Admin notes are absent. |
| 5:00-6:15 | **Live AI:** Open Recommendations and generate. Require three ranked results, scores/match facts, nonempty explanations, and **AI generated**. Show history/provider/model metadata. Generate again only after explaining that an unchanged batch is deliberately reused. |
| 6:15-7:15 | **Workflow:** Show Pending -> Approved application status and the assigned team, leader, and skill gaps. Show that closed-project history remains in My Work with a **Project closed** label and no dead detail link. Mention duplicate, capacity, unapproved-member, and second-active-team rejection rules. |
| 7:15-8:00 | **Privacy and resilience:** Show opaque teammate suggestions without email/goals/notes. Use the recorded local Fallback evidence; explain that browsing, applications, and teams remain operational and that production credentials are never changed for outage testing. |
| 8:00-9:00 | **Tests, security, and cloud:** Show 53 passing tests, Docker evidence, secret scan, HTTPS redirect, CORS allowlist, JWT 401/403, hashed passwords, minimized AI payload, Railway Dockerfile build, PostgreSQL health, and current migrations. |
| 9:00-10:00 | **Close and Q&A:** Return to the vision, state the deliberate scope limits, and invite questions. |

## Code examples to keep ready

- `SKillMatchBE/Controllers/RecommendationsController.cs` - thin HTTP/role boundary.
- `SKillMatchBE/Services/RecommendationService.cs` - orchestration, stable ranking,
  privacy filtering, persistence, and fallback.
- `SKillMatchBE/Repositories/RecommendationRepository.cs` - EF Core data access.
- `SKillMatchBE/Data/SkillMatchDbContext.cs` - relationships, indexes, and constraints.
- `SKillMatchBE/Recommendations/OpenAIRecommendationProvider.cs` - time-bounded,
  structured Responses API adapter.
- `SkillMatchBE.Tests/Unit/RecommendationServiceTests.cs` - normal, boundary,
  privacy, reuse, and error behavior.

## Technical Q&A prompts

- **Why a layered monolith?** It makes responsibilities and testing clear without
  adding distributed-system complexity to a course-sized deployment.
- **Why deterministic ranking plus OpenAI?** Scores remain stable and auditable;
  OpenAI adds personalized explanation without deciding admissions or team status.
- **What is sent to OpenAI?** Skills/interests and project match facts only—not
  identity, email, goals, application notes, credentials, or JWTs.
- **What happens during an AI outage?** A labeled deterministic explanation is
  returned; authentication, projects, applications, and teams remain independent.
- **How is authorization enforced?** Signed expiring JWTs, controller role policies,
  and service ownership rules; hiding a button is never the security boundary.
- **How is data integrity protected?** Service validation, serializable workflow
  transactions, unique indexes, restrictive relationships, and PostgreSQL.
- **How is Railway different from local Docker?** Railway builds the same backend
  Dockerfile, injects private PostgreSQL/runtime variables, and routes HTTPS to the
  image's port 8080; the frontend is a direct Vite/Caddy deployment.
- **What would scale next?** Separate migration execution before multiple API
  replicas, explicit course-cycle data, user administration, invitations, and more
  operational monitoring—not microservices by default.

## Stop conditions

Do not claim the final project is complete if health is unhealthy, a protected role
boundary fails, the live result says Fallback, an explanation is empty, or the UI is
unusable at mobile width. Switch to recorded evidence, explain the failing external
dependency accurately, and preserve time for architecture/tests; recorded fallback
does not replace Samuel's required live manual acceptance.
