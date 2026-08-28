import { useEffect, useState } from 'react'
import type { AdminAccessResponse } from '../auth/types'
import { useAuth } from '../auth/useAuth'

export function AdminPage() {
  const { authenticatedRequest } = useAuth()
  const [result, setResult] = useState<AdminAccessResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isActive = true
    authenticatedRequest<AdminAccessResponse>('/api/admin/auth-check')
      .then((response) => { if (isActive) setResult(response) })
      .catch(() => { if (isActive) setError('The API could not confirm Admin access.') })
    return () => { isActive = false }
  }, [authenticatedRequest])

  return (
    <main className="mx-auto min-h-[65vh] max-w-6xl px-5 py-12 sm:px-8 sm:py-16">
      <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Admin only</p>
      <h1 className="mt-3 text-4xl font-black tracking-tight">Instructor workspace</h1>
      <div className="mt-8 max-w-2xl rounded-2xl border border-white/10 bg-white/[0.06] p-6">
        {!result && !error && <p role="status">Confirming authorization…</p>}
        {result && <p className="font-semibold text-emerald-300">{result.message}</p>}
        {error && <p className="text-red-200" role="alert">{error}</p>}
      </div>
    </main>
  )
}
