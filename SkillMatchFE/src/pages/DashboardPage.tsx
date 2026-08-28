import { useAuth } from '../auth/useAuth'
import { Link } from 'react-router-dom'

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <main className="mx-auto min-h-[65vh] max-w-6xl px-5 py-12 sm:px-8 sm:py-16">
      <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Authenticated workspace</p>
      <h1 className="mt-3 text-4xl font-black tracking-tight">Welcome to SkillMatch</h1>
      <p className="mt-4 max-w-2xl text-lg leading-8 text-slate-300">
        You are signed in as <span className="font-bold text-white">{user?.email}</span>.
      </p>
      <section className="mt-10 max-w-xl rounded-2xl border border-white/10 bg-white/[0.06] p-6">
        <h2 className="text-xl font-bold">Account access</h2>
        <dl className="mt-5 grid gap-4 text-sm">
          <div className="flex justify-between gap-6 border-t border-white/10 pt-4"><dt className="text-slate-400">Role</dt><dd className="font-semibold text-cyan-200">{user?.role}</dd></div>
          <div className="flex justify-between gap-6 border-t border-white/10 pt-4"><dt className="text-slate-400">Session</dt><dd className="font-semibold text-emerald-300">Verified</dd></div>
        </dl>
      </section>
      <div className="mt-8 flex flex-wrap gap-3">
        <Link className="rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 hover:bg-cyan-300" to="/projects">
          Browse projects
        </Link>
        {user?.role === 'Student' && (
          <Link className="rounded-xl border border-white/15 px-5 py-3 font-bold hover:bg-white/10" to="/profile">
            Complete profile
          </Link>
        )}
      </div>
    </main>
  )
}
