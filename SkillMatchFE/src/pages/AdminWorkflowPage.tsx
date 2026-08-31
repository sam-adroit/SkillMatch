import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiError } from '../auth/api'
import type { AdminDashboard, AdminProject, ProjectApplication, Team, TeamSkillGap } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { toast } from 'sonner'

const inputClass = 'min-h-12 w-full rounded-xl border border-white/15 bg-slate-950 px-4 py-3 text-white'
const decisions = ['Approved', 'Rejected', 'Waitlisted'] as const

export function AdminWorkflowPage() {
  const { authenticatedRequest } = useAuth()
  const [dashboard, setDashboard] = useState<AdminDashboard | null>(null)
  const [applications, setApplications] = useState<ProjectApplication[]>([])
  const [approvedApplications, setApprovedApplications] = useState<ProjectApplication[]>([])
  const [projects, setProjects] = useState<AdminProject[]>([])
  const [teams, setTeams] = useState<Team[]>([])
  const [skillGaps, setSkillGaps] = useState<Record<string, TeamSkillGap>>({})
  const [statusFilter, setStatusFilter] = useState('')
  const [projectFilter, setProjectFilter] = useState('')
  const [teamProjectId, setTeamProjectId] = useState('')
  const [teamName, setTeamName] = useState('')
  const [memberIds, setMemberIds] = useState<string[]>([])
  const [leaderId, setLeaderId] = useState('')
  const [loadError, setLoadError] = useState<string | null>(null)

  const loadApplications = useCallback(async () => {
    const query = new URLSearchParams()
    if (statusFilter) query.set('status', statusFilter)
    if (projectFilter) query.set('projectId', projectFilter)
    setApplications(await authenticatedRequest<ProjectApplication[]>(`/api/admin/applications?${query}`))
  }, [authenticatedRequest, projectFilter, statusFilter])

  const loadAll = useCallback(async () => {
    try {
      const [counts, loadedProjects, loadedTeams, loadedApproved] = await Promise.all([
        authenticatedRequest<AdminDashboard>('/api/admin/dashboard'),
        authenticatedRequest<AdminProject[]>('/api/admin/projects'),
        authenticatedRequest<Team[]>('/api/teams'),
        authenticatedRequest<ProjectApplication[]>('/api/admin/applications?status=Approved'),
      ])
      setDashboard(counts); setProjects(loadedProjects); setTeams(loadedTeams); setApprovedApplications(loadedApproved)
      const gaps = await Promise.all(loadedTeams.map((team) => authenticatedRequest<TeamSkillGap>(`/api/teams/${team.id}/skill-gaps`)))
      setSkillGaps(Object.fromEntries(gaps.map((gap) => [gap.teamId, gap])))
      setTeamProjectId((current) => current || loadedProjects.find((item) => item.status === 'Published')?.id || '')
      await loadApplications()
    } catch { setLoadError('Unable to load applications, teams, and dashboard counts.') }
  }, [authenticatedRequest, loadApplications])

  useEffect(() => { void loadAll() }, [loadAll])
  useEffect(() => { void loadApplications().catch(() => setLoadError('Unable to filter applications.')) }, [loadApplications])

  const selectedTeam = teams.find((team) => team.projectId === teamProjectId)
  const approvedCandidates = useMemo(() => approvedApplications.filter((item) => item.projectId === teamProjectId), [approvedApplications, teamProjectId])

  useEffect(() => {
    if (selectedTeam) {
      setTeamName(selectedTeam.name); setMemberIds(selectedTeam.members.map((member) => member.studentId)); setLeaderId(selectedTeam.members.find((member) => member.isLeader)?.studentId ?? '')
    } else { setTeamName(''); setMemberIds([]); setLeaderId('') }
  }, [selectedTeam])

  function showError(caught: unknown, fallback: string) {
    toast.error(caught instanceof ApiError ? caught.message : fallback)
  }

  async function decide(id: string, status: typeof decisions[number]) {
    try {
      await authenticatedRequest(`/api/admin/applications/${id}/decision`, { method: 'PATCH', body: JSON.stringify({ status, decisionNote: '' }) })
      toast.success(`Application marked ${status}.`); await loadAll()
    } catch (caught) { showError(caught, 'Unable to update the application.') }
  }

  function toggleMember(id: string) {
    setMemberIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id])
  }

  async function saveTeam(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const body = selectedTeam
      ? { name: teamName, leaderStudentId: leaderId, memberStudentIds: memberIds }
      : { projectId: teamProjectId, name: teamName, leaderStudentId: leaderId, memberStudentIds: memberIds }
    try {
      await authenticatedRequest(selectedTeam ? `/api/admin/teams/${selectedTeam.id}` : '/api/admin/teams', {
        method: selectedTeam ? 'PUT' : 'POST', body: JSON.stringify(body),
      })
      toast.success(selectedTeam ? 'Team updated.' : 'Team created.'); await loadAll()
    } catch (caught) { showError(caught, 'Unable to save the team.') }
  }

  const cards = dashboard ? [
    ['Students', dashboard.students], ['Projects', dashboard.projects], ['Active teams', dashboard.teams],
    ['Pending applications', dashboard.pendingApplications], ['Unassigned students', dashboard.unassignedStudents],
  ] : []

  return <main className="mx-auto min-h-[65vh] max-w-7xl px-5 py-10 sm:px-8 sm:py-14">
    <p className="text-sm font-bold uppercase tracking-[0.18em] text-violet-300">Admin workflow</p><h1 className="mt-3 text-4xl font-black tracking-tight">Applications, teams, and dashboard</h1>
    {loadError && <p className="mt-6 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{loadError}</p>}
    <section className="mt-8 grid gap-3 sm:grid-cols-2 xl:grid-cols-5" aria-label="Dashboard counts">{cards.map(([label, value]) => <article className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={label}><p className="text-sm text-slate-400">{label}</p><p className="mt-2 text-3xl font-black text-cyan-200">{value}</p></article>)}</section>

    <section className="mt-10"><h2 className="text-2xl font-black">Review applications</h2><div className="mt-4 grid gap-3 rounded-2xl border border-white/10 p-4 sm:grid-cols-2"><label className="font-semibold">Status<select className={`${inputClass} mt-2`} value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}><option value="">All statuses</option><option>Pending</option><option>Approved</option><option>Rejected</option><option>Waitlisted</option></select></label><label className="font-semibold">Project<select className={`${inputClass} mt-2`} value={projectFilter} onChange={(event) => setProjectFilter(event.target.value)}><option value="">All projects</option>{projects.map((project) => <option key={project.id} value={project.id}>{project.title}</option>)}</select></label></div>
      {applications.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">No applications match these filters.</p> : <div className="mt-4 grid gap-4 lg:grid-cols-2">{applications.map((item) => <article className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={item.id}><p className="text-sm font-bold uppercase tracking-wider text-cyan-300">{item.status}</p><h3 className="mt-2 text-xl font-black">{item.projectTitle}</h3><p className="mt-2 text-slate-300">{item.studentName}</p>{item.note && <p className="mt-3 rounded-xl bg-slate-950 p-3 text-slate-300">{item.note}</p>}<div className="mt-4 flex flex-wrap gap-2">{decisions.map((status) => <button className="min-h-11 rounded-lg border border-white/20 px-4 font-bold hover:bg-white/10" key={status} onClick={() => void decide(item.id, status)}>{status}</button>)}</div></article>)}</div>}
    </section>

    <section className="mt-10 rounded-2xl border border-white/10 bg-white/[0.05] p-5 sm:p-8"><h2 className="text-2xl font-black">Create or manage a team</h2><form className="mt-5 grid gap-5" onSubmit={saveTeam}>
      <label className="font-semibold">Published project<select className={`${inputClass} mt-2`} required value={teamProjectId} onChange={(event) => setTeamProjectId(event.target.value)}><option value="">Choose a project</option>{projects.filter((item) => item.status === 'Published').map((project) => <option key={project.id} value={project.id}>{project.title}</option>)}</select></label>
      <label className="font-semibold">Team name<input className={`${inputClass} mt-2`} minLength={2} required value={teamName} onChange={(event) => setTeamName(event.target.value)} /></label>
      <fieldset><legend className="font-semibold">Approved members</legend>{approvedCandidates.length === 0 ? <p className="mt-3 text-slate-400">Approve applications for this project before creating its team.</p> : <div className="mt-3 grid gap-2 md:grid-cols-2">{approvedCandidates.map((item) => <label className="flex min-h-12 items-center gap-3 rounded-xl bg-slate-950 px-4" key={item.studentId}><input checked={memberIds.includes(item.studentId)} type="checkbox" onChange={() => toggleMember(item.studentId)} />{item.studentName}</label>)}</div>}</fieldset>
      <label className="font-semibold">Team leader<select className={`${inputClass} mt-2`} required value={leaderId} onChange={(event) => setLeaderId(event.target.value)}><option value="">Choose a selected member</option>{approvedCandidates.filter((item) => memberIds.includes(item.studentId)).map((item) => <option key={item.studentId} value={item.studentId}>{item.studentName}</option>)}</select></label>
      <button className="min-h-12 rounded-xl bg-violet-400 px-5 font-bold text-slate-950" type="submit">{selectedTeam ? 'Update team' : 'Create team'}</button>
    </form></section>

    <section className="mt-10"><h2 className="text-2xl font-black">Active teams</h2>{teams.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">No active teams yet.</p> : <div className="mt-4 grid gap-4 lg:grid-cols-2">{teams.map((team) => { const gap = skillGaps[team.id]; return <article className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={team.id}><p className="text-sm font-bold uppercase tracking-wider text-emerald-300">{team.projectTitle}</p><h3 className="mt-2 text-xl font-black">{team.name}</h3><p className="mt-2 text-slate-400">{team.members.length} / {team.maximumSize}</p><ul className="mt-3 grid gap-2">{team.members.map((member) => <li className="rounded-lg bg-slate-950 p-3" key={member.studentId}>{member.name}{member.isLeader && <strong className="ml-2 text-cyan-300">Leader</strong>}</li>)}</ul>{gap && <div className="mt-4 rounded-xl bg-slate-950 p-4"><h4 className="font-black">Required-skill coverage</h4><p className="mt-2 text-sm text-slate-300">Covered: {gap.coveredSkills.join(', ') || 'None yet'}</p><p className={`mt-2 text-sm font-bold ${gap.missingSkills.length ? 'text-amber-200' : 'text-emerald-200'}`}>{gap.missingSkills.length ? `Missing: ${gap.missingSkills.join(', ')}` : 'No required skill gaps.'}</p></div>}<button className="mt-4 min-h-11 rounded-lg border border-white/20 px-4" onClick={() => setTeamProjectId(team.projectId)}>Manage team</button></article> })}</div>}</section>
  </main>
}
