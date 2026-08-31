# Admin / instructor guide

Use the deployed app at <https://web-production-ff322.up.railway.app> or the local
frontend at <http://localhost:5173>. Use the configured demo Admin email/password
from runtime configuration; credentials are never committed to this repository.
The navigation identifies the signed-in Admin by full name; email remains the login
credential only.

## Catalog and project setup

1. Log in as Admin and open **Admin workspace**.
2. Create or maintain skills, interests, and categories. Names are unique without
   regard to case; a lookup that is already referenced cannot be deleted.
3. Create a project with title, description, category, difficulty, required skills,
   minimum/preferred/maximum team sizes, and optional private Admin notes.
4. Publish the project so Students can browse and apply. Close it to stop new
   applications. Only Draft projects without blocking references can be deleted.

## Review applications

1. Open **Applications, teams, and dashboard**.
2. Filter applications by status or project.
3. Select **Approved**, **Rejected**, or **Waitlisted** and save the decision.
4. Confirm the status and decision timestamp update. Approval is rejected when the
   project is closed, capacity is reached, or the Student already has an active team.

## Create and maintain teams

1. Select a Published project and enter a team name.
2. Select only Students whose application to that project is Approved; candidates
   are labeled by full name rather than email.
3. Choose a leader by full name who is also in the selected member list and create
   the team.
4. Update membership as needed. The API enforces maximum size, approved application,
   one active team per Student in the implicit course cycle, and one team per project.
5. Review the team's covered and missing required skills. Adding a member with a
   missing skill removes that gap after refresh.

## Dashboard and authorization checks

The dashboard reports Students, projects, teams, Pending applications, and
unassigned Students. Counts refresh after application/team changes. Admin may inspect
any team's skill gaps, but project recommendation generation is Student-only.

For API demonstrations, log in through Swagger, copy only the returned `token`,
select **Authorize**, and paste the token exactly as the dialog requests.
