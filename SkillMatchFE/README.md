# SkillMatch frontend

React 19, TypeScript, Vite, and Tailwind CSS power the SkillMatch web interface.

From the repository root:

```powershell
Push-Location SkillMatchFE
npm install
npm run dev
Pop-Location
```

Copy `.env.example` to `.env` before local development. `VITE_API_URL` is the
public base URL of the SkillMatch backend and must never contain secrets.

Verification:

```powershell
npm run lint --prefix SkillMatchFE
npm run build --prefix SkillMatchFE
```

Student routes include the profile, published-project catalog/detail, project
application form, **Recommendations** ranked-project/history/teammate workspace,
and **My work** application/team status plus skill-gap view. Admins use the
catalog/project workspace plus the responsive applications, team editor, and count
dashboard with team skill coverage at `/admin/workflows`. Recommendation cards
label live `AI generated` and deterministic `Fallback` results distinctly. The
browser never receives an OpenAI key. The API enforces every role and workflow rule; UI
visibility is not treated as authorization.

Usage and presentation references:

- [Student guide](../docs/student-guide.md)
- [Admin / instructor guide](../docs/admin-guide.md)
- [End-to-end demo checklist](../docs/demo-checklist.md)
- [Responsive architecture diagrams](../docs/diagrams/README.md)
