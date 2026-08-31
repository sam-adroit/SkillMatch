# End-to-end demo checklist

This is the concise 8-10 minute presentation path. Use prepared demo data so every
step is visible without entering secrets on screen.

Use the timed rubric order and technical talking points in
`docs/presentation-checklist.md`. Current public deployment, fallback, and rollback
evidence is recorded in `docs/presentation-evidence.md`.

## Pre-demo checks

- Production Web and API deployments report `SUCCESS` in Railway.
- `/health/database` reports healthy PostgreSQL.
- At least one Published project and two complete Student profiles exist.
- The backend-only OpenAI key is configured; production demo seeding is disabled
  unless intentionally enabled for the presentation environment.

## Main flow

1. **Admin project setup** - Log in as Admin, show dashboard totals, create or open a
   project with category, required skills, capacity, and Published status.
2. **Student profile and search** - Log in as Student, show a complete profile,
   filter Projects, and open the prepared project. Point out that Admin notes are absent.
3. **Live AI recommendation** - Open Recommendations, generate a result, and show
   score, matched/missing skills, nonempty explanation, model/provider metadata, and
   **AI generated** status. Generate again to demonstrate stored-batch reuse/history.
4. **Application boundary** - Apply to the project, show Pending in My work, and try
   the duplicate action to demonstrate conflict handling.
5. **Admin decision and team** - Return as Admin, approve the application, create or
   update the project team, designate a leader, and show dashboard count changes.
6. **Student team view** - Return as Student and show Approved status, team members,
   leader, and covered/missing skills in My work.
7. **Teammate privacy** - Show teammate suggestions and confirm no email, goals,
   application note, or unavailable/assigned Student appears.
8. **Resilience** - Describe or demonstrate the safe local empty-key container. Show
   the visible Fallback state and confirm Projects/My work remain available. Do not
   alter the Railway production key.

During each user-triggered save/decision/team/recommendation action, confirm a
closable success or error toast appears without scrolling. Keep form validation and
page-loading failures visible inline. Confirm navigation, applications, member and
leader selection, and team lists use full names while email is used only to log in.

## Boundary evidence to mention

- Student calling an Admin endpoint receives 403.
- Closed project, duplicate application, capacity overflow, unapproved team member,
  and second active-team assignment are rejected.
- Closing a project removes it from Student browsing while My Work keeps the
  application/decision history, shows **Project closed**, and provides no dead
  project-detail link.
- A 375px viewport has no page-level horizontal scrolling or inaccessible actions.
- Automated test, Docker, Railway, and diagram evidence is recorded in
  `docs/test-evidence.md`.
