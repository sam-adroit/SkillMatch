import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../auth/api'
import type { Project, ProjectApplication } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { toast } from 'sonner'

export function ProjectDetailPage() {
  const { projectId } = useParams()
  const { authenticatedRequest, user } = useAuth()
  const [project, setProject] = useState<Project | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [application, setApplication] = useState<ProjectApplication | null>(null)
  const [note, setNote] = useState('')

  useEffect(() => {
    if (!projectId) return
    authenticatedRequest<Project>(`/api/projects/${projectId}`)
      .then(setProject)
      .catch(() => setLoadError('This project is unavailable or has not been published.'))
  }, [authenticatedRequest, projectId])

  useEffect(() => {
    if (!projectId || user?.role !== 'Student') return
    authenticatedRequest<ProjectApplication[]>('/api/applications')
      .then((items) => setApplication(items.find((item) => item.projectId === projectId) ?? null))
      .catch(() => setLoadError('Unable to load your application status.'))
  }, [authenticatedRequest, projectId, user?.role])

  async function apply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      const created = await authenticatedRequest<ProjectApplication>(`/api/projects/${projectId}/applications`, {
        method: 'POST', body: JSON.stringify({ note }),
      })
      setApplication(created); toast.success('Application submitted successfully.')
    } catch (caught) {
      toast.error(caught instanceof ApiError ? caught.message : 'Unable to submit your application.')
    }
  }

  return (
    <main className="mx-auto min-h-[65vh] max-w-4xl px-5 py-10 sm:px-8 sm:py-14">
      <Link className="font-bold text-cyan-300 hover:text-cyan-200" to="/projects">← Back to projects</Link>
      {loadError && <p className="mt-8 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{loadError}</p>}
      {!project && !loadError && <p className="mt-8" role="status">Loading project…</p>}
      {project && <article className="mt-8 rounded-2xl border border-white/10 bg-white/[0.05] p-6 sm:p-9">
        <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">{project.category.name} · {project.difficulty}</p>
        <h1 className="mt-3 text-4xl font-black tracking-tight">{project.title}</h1>
        <p className="mt-6 whitespace-pre-wrap text-lg leading-8 text-slate-300">{project.description}</p>
        <div className="mt-8 rounded-xl bg-slate-950 p-5"><h2 className="font-bold">Team size</h2><p className="mt-2 text-slate-300">Minimum {project.minimumTeamSize}, preferred {project.preferredTeamSize}, maximum {project.maximumTeamSize}</p></div>
        <h2 className="mt-8 text-xl font-black">Required skills</h2>
        <div className="mt-3 flex flex-wrap gap-2">{project.requiredSkills.map((skill) => <span className="rounded-full bg-cyan-300/10 px-3 py-2 text-cyan-100" key={skill.id}>{skill.name}</span>)}</div>
        {user?.role === 'Student' && <section className="mt-9 border-t border-white/10 pt-8"><h2 className="text-2xl font-black">Your application</h2>
          {application ? <div className="mt-4 rounded-xl border border-cyan-300/30 bg-cyan-300/10 p-5"><p className="font-bold text-cyan-100">Status: {application.status}</p>{application.note && <p className="mt-2 text-slate-300">{application.note}</p>}{application.decisionNote && <p className="mt-2 text-sm text-slate-400">Instructor: {application.decisionNote}</p>}</div>
            : <form className="mt-4 grid gap-4" onSubmit={apply}><label className="font-semibold">Optional note<textarea className="mt-2 min-h-28 w-full rounded-xl border border-white/15 bg-slate-950 px-4 py-3" maxLength={1000} value={note} onChange={(event) => setNote(event.target.value)} /></label><button className="min-h-12 rounded-xl bg-cyan-400 px-5 font-bold text-slate-950" type="submit">Apply to this project</button></form>}
        </section>}
      </article>}
    </main>
  )
}
