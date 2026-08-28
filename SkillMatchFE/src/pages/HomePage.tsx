import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

const foundations = [
  ['01', 'Student experience', 'Build a verified profile and discover projects that fit your strengths.'],
  ['02', 'Project matching', 'Understand ranked recommendations through clear match details.'],
  ['03', 'Instructor tools', 'Manage projects, applications, and balanced teams in one place.'],
]

export function HomePage() {
  const { user } = useAuth()

  return (
    <main>
      <section className="relative isolate overflow-hidden">
        <div
          className="absolute inset-x-0 top-0 -z-10 h-96 bg-[radial-gradient(circle_at_top_right,rgba(34,211,238,0.20),transparent_45%)]"
          aria-hidden="true"
        />
        <div className="mx-auto grid max-w-6xl gap-12 px-5 py-16 sm:px-8 sm:py-24 lg:grid-cols-[1.25fr_0.75fr] lg:items-center">
          <div>
            <p className="mb-5 inline-flex rounded-full border border-cyan-300/30 bg-cyan-300/10 px-3 py-1 text-sm font-semibold text-cyan-200">
              Secure student and instructor access
            </p>
            <h1 className="max-w-3xl text-4xl font-black tracking-tight text-balance sm:text-6xl">
              Find the project where your skills can make an impact.
            </h1>
            <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-300">
              SkillMatch AI helps students discover fitting projects and gives
              instructors a secure workspace for building balanced teams.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link
                className="inline-flex min-h-12 items-center justify-center rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 hover:bg-cyan-300"
                to={user ? '/dashboard' : '/register'}
              >
                {user ? 'Open dashboard' : 'Create a Student account'}
              </Link>
              {!user && (
                <Link
                  className="inline-flex min-h-12 items-center justify-center rounded-xl border border-white/15 px-5 py-3 font-bold text-white hover:bg-white/10"
                  to="/login"
                >
                  Log in
                </Link>
              )}
            </div>
          </div>

          <aside className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 sm:p-8">
            <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">
              Authentication ready
            </p>
            <h2 className="mt-3 text-2xl font-bold">Your workspace, protected</h2>
            <p className="mt-3 leading-7 text-slate-300">
              Passwords are securely hashed, sessions use expiring tokens, and
              Student/Admin permissions are enforced by the API.
            </p>
          </aside>
        </div>
      </section>

      <section className="border-y border-white/10 bg-slate-900/60">
        <div className="mx-auto max-w-6xl px-5 py-14 sm:px-8 sm:py-20">
          <h2 className="text-3xl font-black tracking-tight sm:text-4xl">
            One secure foundation for every workflow
          </h2>
          <div className="mt-9 grid gap-4 md:grid-cols-3">
            {foundations.map(([number, title, description]) => (
              <article key={number} className="rounded-2xl border border-white/10 bg-slate-950 p-6">
                <span className="text-sm font-black text-cyan-300">{number}</span>
                <h3 className="mt-5 text-xl font-bold">{title}</h3>
                <p className="mt-3 leading-7 text-slate-400">{description}</p>
              </article>
            ))}
          </div>
        </div>
      </section>
    </main>
  )
}
