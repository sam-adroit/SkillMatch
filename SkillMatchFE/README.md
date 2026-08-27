# SkillMatch frontend

React 19, TypeScript, Vite, and Tailwind CSS power the SkillMatch web interface.

From the repository root:

```powershell
npm install --prefix SkillMatchFE
npm run dev --prefix SkillMatchFE
```

Copy `.env.example` to `.env` before local development. `VITE_API_URL` is the
public base URL of the SkillMatch backend and must never contain secrets.

Verification:

```powershell
npm run lint --prefix SkillMatchFE
npm run build --prefix SkillMatchFE
```
