import type { ReactNode } from 'react'

export function AuthPageLayout({
  eyebrow,
  title,
  description,
  children,
}: {
  eyebrow: string
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <main className="relative isolate overflow-hidden">
      <div
        className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.14),transparent_42%)]"
        aria-hidden="true"
      />
      <div className="mx-auto grid min-h-[calc(100vh-14rem)] max-w-6xl place-items-center px-5 py-12 sm:px-8">
        <section className="w-full max-w-lg rounded-3xl border border-white/10 bg-slate-900/90 p-6 shadow-2xl shadow-black/30 sm:p-9">
          <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">
            {eyebrow}
          </p>
          <h1 className="mt-3 text-3xl font-black tracking-tight sm:text-4xl">
            {title}
          </h1>
          <p className="mt-3 leading-7 text-slate-300">{description}</p>
          <div className="mt-8">{children}</div>
        </section>
      </div>
    </main>
  )
}

export const inputClassName =
  'mt-2 min-h-12 w-full rounded-xl border border-white/15 bg-slate-950 px-4 py-3 text-base text-white placeholder:text-slate-600 focus:border-cyan-300 focus:outline-none focus:ring-2 focus:ring-cyan-300/30'

export const primaryButtonClassName =
  'inline-flex min-h-12 w-full items-center justify-center rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 hover:bg-cyan-300 disabled:cursor-not-allowed disabled:opacity-60'
