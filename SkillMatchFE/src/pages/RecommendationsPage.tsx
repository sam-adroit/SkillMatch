import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ApiError } from '../auth/api'
import type {
  RecommendationBatch,
  RecommendationHistory,
  TeammateSuggestion,
} from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { toast } from 'sonner'

function skillList(label: string, values: string[], tone: string) {
  return <div><p className="text-sm font-bold text-slate-300">{label}</p><div className="mt-2 flex flex-wrap gap-2">
    {values.length === 0 ? <span className="text-sm text-slate-500">None</span> : values.map((value) => <span className={`rounded-full px-3 py-1 text-sm ${tone}`} key={value}>{value}</span>)}
  </div></div>
}

export function RecommendationsPage() {
  const { authenticatedRequest } = useAuth()
  const [batch, setBatch] = useState<RecommendationBatch | null>(null)
  const [history, setHistory] = useState<RecommendationHistory[]>([])
  const [teammates, setTeammates] = useState<TeammateSuggestion[]>([])
  const [loading, setLoading] = useState(true)
  const [generating, setGenerating] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)

  const loadSupportingResults = useCallback(async () => {
    const [savedHistory, suggestions] = await Promise.all([
      authenticatedRequest<RecommendationHistory[]>('/api/recommendations/history'),
      authenticatedRequest<TeammateSuggestion[]>('/api/recommendations/teammates'),
    ])
    setHistory(savedHistory)
    setTeammates(suggestions)
  }, [authenticatedRequest])

  useEffect(() => {
    loadSupportingResults()
      .catch((caught) => setLoadError(caught instanceof ApiError ? caught.message : 'Unable to load recommendation history and teammate suggestions.'))
      .finally(() => setLoading(false))
  }, [loadSupportingResults])

  async function generate() {
    setGenerating(true)
    try {
      const result = await authenticatedRequest<RecommendationBatch>('/api/recommendations/projects', { method: 'POST' })
      setBatch(result)
      setHistory(await authenticatedRequest<RecommendationHistory[]>('/api/recommendations/history'))
      toast.success(result.providerStatus === 'Fallback' ? 'Fallback recommendations are ready.' : 'Recommendations generated successfully.')
    } catch (caught) {
      toast.error(caught instanceof ApiError ? caught.message : 'Unable to generate recommendations right now.')
    } finally { setGenerating(false) }
  }

  return <main className="mx-auto min-h-[65vh] max-w-6xl px-5 py-10 sm:px-8 sm:py-14">
    <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Personalized matching</p>
    <div className="mt-3 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
      <div><h1 className="text-4xl font-black tracking-tight">Recommendations</h1><p className="mt-3 max-w-2xl leading-7 text-slate-300">Deterministic fit scores stay explainable. OpenAI adds a short personalized reason without receiving your identity, goals, or application notes.</p></div>
      <button className="min-h-12 shrink-0 rounded-xl bg-cyan-400 px-5 font-black text-slate-950 disabled:cursor-not-allowed disabled:opacity-60" disabled={generating} onClick={() => void generate()}>{generating ? 'Generating…' : 'Generate recommendations'}</button>
    </div>
    {loading && <p className="mt-8" role="status">Loading recommendation workspace…</p>}
    {loadError && <p className="mt-6 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{loadError} <Link className="font-bold underline" to="/profile">Review profile</Link></p>}
    {batch?.providerStatus === 'Fallback' && <p className="mt-6 rounded-xl border border-amber-300/30 bg-amber-400/10 p-4 text-amber-100" role="status"><strong>Fallback mode:</strong> OpenAI was unavailable, so these explanations are deterministic. Project browsing, applications, and teams still work.</p>}
    {batch?.providerStatus === 'AiGenerated' && <p className="mt-6 rounded-xl border border-emerald-300/30 bg-emerald-400/10 p-4 text-emerald-100" role="status"><strong>AI-generated explanations ready.</strong>{batch.reused ? ' Reused because your profile and the ranked projects have not changed.' : ' A new result was saved to your history.'}</p>}

    <section className="mt-10"><h2 className="text-2xl font-black">Ranked projects</h2>
      {!batch ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">Generate recommendations to rank the current published projects.</p> : batch.results.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">No published projects are available right now.</p> : <div className="mt-4 grid gap-5 lg:grid-cols-3">{batch.results.map((item, index) => <article className="flex flex-col rounded-2xl border border-cyan-300/20 bg-cyan-300/[0.05] p-5" key={item.projectId}>
        <div className="flex items-center justify-between gap-3"><span className="font-black text-cyan-300">#{index + 1}</span><span className="rounded-full bg-white/10 px-3 py-1 font-black">{item.score.toFixed(2)} / 100</span></div>
        <h3 className="mt-4 text-xl font-black">{item.projectTitle}</h3><p className="mt-3 grow leading-7 text-slate-200">{item.explanation}</p>
        <div className="mt-5 grid gap-4">{skillList('Matched skills', item.matchedSkills, 'bg-emerald-300/15 text-emerald-200')}{skillList('Growth areas', item.missingSkills, 'bg-amber-300/15 text-amber-200')}</div>
        <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-white/10 pt-4"><span className={`rounded-full px-3 py-1 text-sm font-bold ${item.providerStatus === 'AiGenerated' ? 'bg-violet-300/15 text-violet-200' : 'bg-amber-300/15 text-amber-200'}`}>{item.providerStatus === 'AiGenerated' ? 'AI generated' : 'Fallback'}</span><Link className="font-bold text-cyan-300" to={`/projects/${item.projectId}`}>View project</Link></div>
      </article>)}</div>}
    </section>

    <section className="mt-12"><h2 className="text-2xl font-black">Available teammate suggestions</h2><p className="mt-2 text-slate-400">Only public match facts are shown. Assigned and inactive Students are excluded.</p>
      {teammates.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">No available teammates match the current course cycle.</p> : <div className="mt-4 grid gap-4 md:grid-cols-2">{teammates.map((item) => <article className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={item.studentId}><div className="flex items-center justify-between gap-3"><h3 className="font-black">{item.displayName}</h3><span className="font-bold text-cyan-200">{item.score.toFixed(2)}</span></div><div className="mt-4 grid gap-3">{skillList('Complementary skills', item.complementarySkills, 'bg-violet-300/15 text-violet-200')}{skillList('Shared skills', item.sharedSkills, 'bg-cyan-300/15 text-cyan-200')}{skillList('Shared interests', item.sharedInterests, 'bg-emerald-300/15 text-emerald-200')}</div></article>)}</div>}
    </section>

    <section className="mt-12"><h2 className="text-2xl font-black">Recommendation history</h2>
      {history.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">No recommendation history yet.</p> : <div className="mt-4 grid gap-3">{history.slice(0, 12).map((item) => <article className="rounded-xl border border-white/10 p-4" key={item.id}><div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"><h3 className="font-bold">{item.projectTitle} · {item.score.toFixed(2)}</h3><span className="text-sm text-slate-400">{item.providerStatus === 'AiGenerated' ? `AI generated · ${item.provider} / ${item.model}` : 'Fallback'} · {new Date(item.createdAt).toLocaleString()}</span></div><p className="mt-2 text-sm leading-6 text-slate-300">{item.explanation}</p></article>)}</div>}
    </section>
  </main>
}
