const apiUrl = import.meta.env.VITE_API_URL?.replace(/\/$/, '')

const foundations = [
  {
    number: '01',
    title: 'Student experience',
    description:
      'A mobile-first workspace prepared for profiles, discovery, and applications.',
  },
  {
    number: '02',
    title: 'Project matching',
    description:
      'A clear home for ranked recommendations and understandable match details.',
  },
  {
    number: '03',
    title: 'Instructor tools',
    description:
      'A consistent shell prepared for project, application, and team management.',
  },
]

function App() {
  const swaggerUrl = apiUrl ? `${apiUrl}/swagger` : undefined

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-white/10 bg-slate-950/95">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-5 py-4 sm:px-8">
          <a className="flex items-center gap-3 rounded-md" href="#top">
            <span
              className="grid size-10 place-items-center rounded-xl bg-cyan-400 font-black text-slate-950"
              aria-hidden="true"
            >
              SM
            </span>
            <span>
              <span className="block text-base font-bold tracking-tight">
                SkillMatch AI
              </span>
              <span className="block text-xs text-slate-400">
                Teams built around potential
              </span>
            </span>
          </a>

          <nav aria-label="Primary navigation">
            <ul className="flex items-center gap-1 text-sm font-semibold text-slate-300">
              <li>
                <a
                  className="block rounded-lg px-3 py-2 hover:bg-white/10"
                  href="#overview"
                >
                  Overview
                </a>
              </li>
              <li>
                <a
                  className="block rounded-lg px-3 py-2 hover:bg-white/10"
                  href="#foundation"
                >
                  Foundation
                </a>
              </li>
            </ul>
          </nav>
        </div>
      </header>

      <main id="top">
        <section id="overview" className="relative isolate overflow-hidden">
          <div
            className="absolute inset-x-0 top-0 -z-10 h-96 bg-[radial-gradient(circle_at_top_right,rgba(34,211,238,0.20),transparent_45%)]"
            aria-hidden="true"
          />
          <div className="mx-auto grid max-w-6xl gap-12 px-5 py-16 sm:px-8 sm:py-24 lg:grid-cols-[1.25fr_0.75fr] lg:items-center">
            <div>
              <p className="mb-5 inline-flex rounded-full border border-cyan-300/30 bg-cyan-300/10 px-3 py-1 text-sm font-semibold text-cyan-200">
                Project collaboration, made intentional
              </p>
              <h1 className="max-w-3xl text-4xl font-black tracking-tight text-balance sm:text-6xl">
                Find the project where your skills can make an impact.
              </h1>
              <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-300">
                SkillMatch AI will help students discover fitting projects and help
                instructors form balanced, capable teams.
              </p>
              <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                <a
                  className="inline-flex min-h-12 items-center justify-center rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 shadow-lg shadow-cyan-950/40 hover:bg-cyan-300"
                  href="#foundation"
                >
                  View the foundation
                </a>
                {swaggerUrl && (
                  <a
                    className="inline-flex min-h-12 items-center justify-center rounded-xl border border-white/15 px-5 py-3 font-bold text-white hover:bg-white/10"
                    href={swaggerUrl}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Open API documentation
                  </a>
                )}
              </div>
            </div>

            <aside className="rounded-3xl border border-white/10 bg-white/[0.06] p-6 shadow-2xl shadow-black/30 sm:p-8">
              <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">
                Baseline ready
              </p>
              <h2 className="mt-3 text-2xl font-bold">A focused starting point</h2>
              <p className="mt-3 leading-7 text-slate-300">
                The responsive interface and API foundation are in place. Student and
                instructor workflows will arrive in the next planned slices.
              </p>
              <dl className="mt-6 grid gap-4 text-sm">
                <div className="flex items-center justify-between gap-4 border-t border-white/10 pt-4">
                  <dt className="text-slate-400">Interface</dt>
                  <dd className="font-semibold text-emerald-300">Responsive</dd>
                </div>
                <div className="flex items-center justify-between gap-4 border-t border-white/10 pt-4">
                  <dt className="text-slate-400">API</dt>
                  <dd className="font-semibold text-emerald-300">Documented</dd>
                </div>
                <div className="flex items-center justify-between gap-4 border-t border-white/10 pt-4">
                  <dt className="text-slate-400">Database</dt>
                  <dd className="font-semibold text-emerald-300">PostgreSQL</dd>
                </div>
              </dl>
            </aside>
          </div>
        </section>

        <section
          id="foundation"
          className="border-y border-white/10 bg-slate-900/60"
        >
          <div className="mx-auto max-w-6xl px-5 py-14 sm:px-8 sm:py-20">
            <div className="max-w-2xl">
              <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">
                Platform foundation
              </p>
              <h2 className="mt-3 text-3xl font-black tracking-tight sm:text-4xl">
                Ready for the core SkillMatch workflows
              </h2>
            </div>
            <div className="mt-9 grid gap-4 md:grid-cols-3">
              {foundations.map(({ number, title, description }) => (
                <article
                  key={number}
                  className="rounded-2xl border border-white/10 bg-slate-950 p-6"
                >
                  <span className="text-sm font-black text-cyan-300">{number}</span>
                  <h3 className="mt-5 text-xl font-bold">{title}</h3>
                  <p className="mt-3 leading-7 text-slate-400">{description}</p>
                </article>
              ))}
            </div>
          </div>
        </section>
      </main>

      <footer className="mx-auto flex max-w-6xl flex-col gap-2 px-5 py-8 text-sm text-slate-400 sm:flex-row sm:items-center sm:justify-between sm:px-8">
        <p>SkillMatch AI</p>
        <p>Built for better project teams.</p>
      </footer>
    </div>
  )
}

export default App
