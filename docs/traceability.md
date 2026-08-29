# Requirements traceability and implemented simplifications

This matrix reconciles the August 2026 SRS with the code at the end of Plan 005.
API authorization and service rules are the acceptance authority; hiding a UI control
is not treated as enforcement.

| Requirement | Implementation evidence | Automated/manual evidence | Status |
|---|---|---|---|
| FR-01 / UC-01 authentication | `AuthController`, `AuthService`, Identity password hashing, JWT bearer roles | Auth unit/API tests; Student 403 from Admin API | Implemented |
| FR-02-03 / UC-02-03 profile | `ProfileController` -> `ProfileService` -> repositories; normalized skill/interest joins | Profile unit tests; Student guide | Implemented with simplified fields |
| FR-04 / UC-04 project management | Admin `ProjectsController`, lookup/project services, unique normalized title and status rules | Project unit tests; Admin guide | Implemented |
| FR-05 / UC-05 browse/search | Server-side Published-project query and filters; Student DTO omits Admin notes | Project service test; demo checklist | Implemented |
| FR-06 / UC-06 applications | Serializable `WorkflowService.ApplyAsync`; unique Student/project database index | Workflow unit tests; apply sequence/VOPC | Implemented |
| FR-07 / UC-07 teams | Admin create/update endpoints; approved-member, capacity, leader, one-team rules | Workflow unit/API tests; Student/Admin guides | Implemented with Admin-controlled membership |
| FR-08 / UC-08 project recommendation | Named deterministic ranking plus one privacy-minimized OpenAI Responses call for top-three explanations; stored history/fallback | Recommendation/provider/smoke tests; recommendation sequence/VOPC; live UI test | Implemented |
| FR-09 / UC-09 teammate suggestion | Deterministic shared-interest/shared/complementary-skill score; availability/privacy filters | Recommendation service test; Student guide | Implemented deterministically |
| FR-10 / UC-10 application review | Admin decision endpoint, capacity/assignment validation, decision timestamps | Workflow unit/API tests; Admin guide | Implemented |
| FR-11 / UC-11 skill gaps | Project-required skills minus union of active member skills | Recommendation service ownership/gap test; My work/Admin UI | Implemented deterministically |
| FR-12 / UC-12 dashboard | Repository aggregate counts through `AdminDashboardController` | Role API tests; Admin guide | Implemented |
| FR-13 demo data | Runtime-gated `DemoDataSeeder`; no committed password | Docker rehearsal and secret scan | Implemented |
| FR-14 timestamps | Profile/project/application/decision/team/recommendation timestamps and migrations | Unit tests plus database migration inspection | Implemented |

## Non-functional traceability

| Requirement | Evidence |
|---|---|
| NFR-01 and NFR-03a responsive/accessibility | Mobile-first Tailwind pages; labeled forms, focus styles, visible status/error states; 375px and desktop manual checks |
| NFR-02 course-size response | Server-side project filters and indexes on project/application/recommendation lookup paths |
| NFR-03 state handling | Problem Details, loading/empty/success/conflict/provider states in API and UI |
| NFR-04 security/privacy | Password hashing, signed/expiring JWT, server-side roles/ownership, runtime secrets, minimized AI payload test |
| NFR-05 integrity | Unique database indexes plus service validation and serializable workflow transactions |
| NFR-06 AI resilience | Bounded OpenAI adapter, deterministic visible fallback, non-AI Docker/API checks |
| NFR-07 and NFR-10 documentation | Root/component READMEs, Student/Admin guides, demo checklist, traceability, diagrams, troubleshooting |
| NFR-08 automated coverage | Presentation-ready results in `docs/test-evidence.md` |
| NFR-09 deployment topology | Canonical backend Dockerfile; frontend direct Railway deployment; container/Railway health evidence |

## Approved or documented simplifications

1. **Profile fields:** The SRS mentions name, program, availability, and bio. The
   implemented matching profile uses account email, experience level, goals,
   preferred technologies, skills, and interests - the smallest field set required
   by the approved product requirements.
2. **Team control:** Although the SRS describes Student/Admin team creation and
   invitations, this release makes membership Admin-controlled with a designated
   Student leader. Student invitations are out of scope.
3. **AI responsibility:** The SRS describes AI ranking, teammate matching, and skill
   gaps. The approved architecture makes ranking, teammate scores, and skill gaps
   deterministic and testable; OpenAI is required for personalized project
   explanations only. AI never approves an application.
4. **Course cycle:** One current course/project cycle is implicit; no `Course` or
   `ProjectCycle` entity exists.
5. **Admin overrides:** The implementation does not bypass capacity or one-team
   integrity rules. Admin resolves conflicts by changing project/team state first.
6. **User administration:** Admin workflows manage the seeded/registered Student
   population through applications and teams; general account CRUD is not included.
7. **SRS document note:** The PDF title page contains the active repository URL, but
   section 1.3 still contains template placeholder wording. Repository documentation
   and this traceability matrix identify the actual implementation source.
