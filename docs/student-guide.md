# Student guide

Use the deployed app at <https://web-production-ff322.up.railway.app> or the local
frontend at <http://localhost:5173>. Demo Student email addresses are documented in
the root README; obtain the configured demo password from the instructor or ignored
runtime configuration.

## Sign in and complete the profile

1. Select **Log in**, enter the Student email/password, and submit.
2. Open **Profile**.
3. Choose an experience level, write goals of at least 10 characters, add at least
   one preferred technology, skill, and interest, then save.
4. Confirm the completeness indicator reaches 100%. A Student can view and edit only
   their own profile.

## Browse and apply

1. Open **Projects** to see Published projects.
2. Filter by search text, skill, category, difficulty, availability, or team size.
3. Open a project and review its required skills and capacity. Admin notes are never
   shown to Students.
4. Enter an optional application note and select **Apply**.
5. Open **My work** and confirm the application is Pending. A duplicate application
   or an application to a closed project is rejected with a visible message.

## Recommendations and teammates

1. Open **Recommendations** and select **Generate recommendations**.
2. Review up to three ranked projects. Each card shows a deterministic score,
   matched skills, growth areas, a short explanation, and either **AI generated** or
   **Fallback** provider status.
3. Generate again without changing the profile to reuse the current stored batch.
   Change a profile skill or technology and generate again to create a fresh batch.
4. Review recommendation history and teammate suggestions. Suggestions show only an
   opaque Student label and shared/complementary matching facts, never email, goals,
   credentials, or application notes.

## Applications, teams, and skill coverage

1. Open **My work** to review application decisions and active team membership.
   If a project was closed after the application, its application and decision
   history remain visible with a **Project closed** label and no project-detail link.
2. For an assigned team, confirm its name, leader, members, project, and covered or
   missing required skills.
3. Students may view only their own team data. Team creation and membership changes
   are Admin-controlled in this release.

## Expected error states

- `401 Unauthorized`: sign in again; the JWT may have expired.
- `403 Forbidden`: the account role does not permit that action.
- `400 Bad Request`: complete the requested profile/form fields.
- `409 Conflict`: the requested state change violates a duplicate, capacity,
  project-status, or team-assignment rule.
- **Fallback** recommendations: OpenAI was unavailable; ordinary browsing,
  applications, and teams continue to work.
