import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { ProjectApplication, Team, TeamSkillGap } from '../auth/types'
import { useAuth } from '../auth/useAuth'

const statusClass: Record<string, string> = {
  Pending: 'bg-amber-300/15 text-amber-200', Approved: 'bg-emerald-300/15 text-emerald-200',
  Rejected: 'bg-red-300/15 text-red-200', Waitlisted: 'bg-violet-300/15 text-violet-200',
}

export function MyWorkPage() {
  const { authenticatedRequest } = useAuth()
  const [applications, setApplications] = useState<ProjectApplication[]>([])
  const [teams, setTeams] = useState<Team[]>([])
  const [skillGaps, setSkillGaps] = useState<Record<string, TeamSkillGap>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([authenticatedRequest<ProjectApplication[]>('/api/applications'), authenticatedRequest<Team[]>('/api/teams')])
      .then(async ([loadedApplications, loadedTeams]) => {
        setApplications(loadedApplications); setTeams(loadedTeams)
        const gaps = await Promise.all(loadedTeams.map((team) => authenticatedRequest<TeamSkillGap>(`/api/teams/${team.id}/skill-gaps`)))
        setSkillGaps(Object.fromEntries(gaps.map((gap) => [gap.teamId, gap])))
      })
      .catch(() => setError('Unable to load your applications and team.'))
      .finally(() => setLoading(false))
  }, [authenticatedRequest])

  return <main className="mx-auto min-h-[65vh] max-w-6xl px-5 py-10 sm:px-8 sm:py-14">
    <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Student workspace</p><h1 className="mt-3 text-4xl font-black tracking-tight">Applications and team</h1>
    {loading && <p className="mt-8" role="status">Loading your work…</p>}{error && <p className="mt-6 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{error}</p>}
    {!loading && !error && <><section className="mt-9"><div className="flex items-end justify-between gap-4"><h2 className="text-2xl font-black">Applications</h2><Link className="font-bold text-cyan-300" to="/projects">Browse projects</Link></div>
      {applications.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">You have not applied to a project yet.</p> : <div className="mt-4 grid gap-4 md:grid-cols-2">{applications.map((item) => <article className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={item.id}><span className={`rounded-full px-3 py-1 text-sm font-bold ${statusClass[item.status]}`}>{item.status}</span><h3 className="mt-4 text-xl font-black">{item.projectTitle}</h3>{item.note && <p className="mt-3 text-slate-300">{item.note}</p>}{item.decisionNote && <p className="mt-3 rounded-lg bg-slate-950 p-3 text-sm text-slate-300">Instructor: {item.decisionNote}</p>}<Link className="mt-4 inline-block font-bold text-cyan-300" to={`/projects/${item.projectId}`}>View project</Link></article>)}</div>}
    </section><section className="mt-10"><h2 className="text-2xl font-black">Active team</h2>{teams.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">You have not been assigned to an active team.</p> : <div className="mt-4 grid gap-4">{teams.map((team) => { const gap = skillGaps[team.id]; return <article className="rounded-2xl border border-emerald-300/20 bg-emerald-300/[0.06] p-5" key={team.id}><p className="text-sm font-bold uppercase tracking-wider text-emerald-300">{team.projectTitle}</p><h3 className="mt-2 text-2xl font-black">{team.name}</h3><p className="mt-2 text-slate-300">{team.members.length} of {team.maximumSize} members</p><ul className="mt-4 grid gap-2 sm:grid-cols-2">{team.members.map((member) => <li className="rounded-xl bg-slate-950 p-3" key={member.studentId}>{member.email}{member.isLeader && <span className="ml-2 text-sm font-bold text-cyan-300">Leader</span>}</li>)}</ul>{gap && <div className="mt-5 border-t border-white/10 pt-4"><h4 className="font-black">Required-skill coverage</h4><p className="mt-2 text-sm text-slate-300">Covered: {gap.coveredSkills.join(', ') || 'None yet'}</p><p className={`mt-2 text-sm font-bold ${gap.missingSkills.length ? 'text-amber-200' : 'text-emerald-200'}`}>{gap.missingSkills.length ? `Missing: ${gap.missingSkills.join(', ')}` : 'No required skill gaps.'}</p></div>}</article> })}</div>}</section></>}
  </main>
}
