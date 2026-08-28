import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import type { Project } from '../auth/types'
import { useAuth } from '../auth/useAuth'

export function ProjectDetailPage() {
  const { projectId } = useParams()
  const { authenticatedRequest } = useAuth()
  const [project, setProject] = useState<Project | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!projectId) return
    authenticatedRequest<Project>(`/api/projects/${projectId}`)
      .then(setProject)
      .catch(() => setError('This project is unavailable or has not been published.'))
  }, [authenticatedRequest, projectId])

  return (
    <main className="mx-auto min-h-[65vh] max-w-4xl px-5 py-10 sm:px-8 sm:py-14">
      <Link className="font-bold text-cyan-300 hover:text-cyan-200" to="/projects">← Back to projects</Link>
      {error && <p className="mt-8 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{error}</p>}
      {!project && !error && <p className="mt-8" role="status">Loading project…</p>}
      {project && <article className="mt-8 rounded-2xl border border-white/10 bg-white/[0.05] p-6 sm:p-9">
        <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">{project.category.name} · {project.difficulty}</p>
        <h1 className="mt-3 text-4xl font-black tracking-tight">{project.title}</h1>
        <p className="mt-6 whitespace-pre-wrap text-lg leading-8 text-slate-300">{project.description}</p>
        <div className="mt-8 rounded-xl bg-slate-950 p-5"><h2 className="font-bold">Team size</h2><p className="mt-2 text-slate-300">Minimum {project.minimumTeamSize}, preferred {project.preferredTeamSize}, maximum {project.maximumTeamSize}</p></div>
        <h2 className="mt-8 text-xl font-black">Required skills</h2>
        <div className="mt-3 flex flex-wrap gap-2">{project.requiredSkills.map((skill) => <span className="rounded-full bg-cyan-300/10 px-3 py-2 text-cyan-100" key={skill.id}>{skill.name}</span>)}</div>
      </article>}
    </main>
  )
}
