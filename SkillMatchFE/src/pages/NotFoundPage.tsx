import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="mx-auto grid min-h-[65vh] max-w-6xl place-items-center px-5 py-12 text-center sm:px-8">
      <div>
        <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">404</p>
        <h1 className="mt-3 text-4xl font-black tracking-tight">Page not found</h1>
        <Link className="mt-7 inline-flex rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 hover:bg-cyan-300" to="/">Return home</Link>
      </div>
    </main>
  )
}
