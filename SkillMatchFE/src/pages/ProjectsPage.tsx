import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import type { Lookup, Project } from '../auth/types'
import { useAuth } from '../auth/useAuth'

const controlClass = 'min-h-12 rounded-xl border border-white/15 bg-slate-950 px-4 py-3 text-white'

export function ProjectsPage() {
  const { authenticatedRequest } = useAuth()
  const [projects, setProjects] = useState<Project[]>([])
  const [skills, setSkills] = useState<Lookup[]>([])
  const [categories, setCategories] = useState<Lookup[]>([])
  const [search, setSearch] = useState('')
  const [skillId, setSkillId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [difficulty, setDifficulty] = useState('')
  const [teamSize, setTeamSize] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([
      authenticatedRequest<Lookup[]>('/api/skills'),
      authenticatedRequest<Lookup[]>('/api/categories'),
    ]).then(([loadedSkills, loadedCategories]) => {
      setSkills(loadedSkills)
      setCategories(loadedCategories)
    }).catch(() => setError('Unable to load project filters.'))
  }, [authenticatedRequest])

  const loadProjects = useCallback(async (query = '') => {
    setLoading(true)
    setError(null)
    try {
      setProjects(await authenticatedRequest<Project[]>(`/api/projects${query}`))
    } catch {
      setError('Unable to load projects.')
    } finally {
      setLoading(false)
    }
  }, [authenticatedRequest])

  useEffect(() => { void loadProjects() }, [loadProjects])

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const query = new URLSearchParams()
    if (search.trim()) query.set('search', search.trim())
    if (skillId) query.set('skillId', skillId)
    if (categoryId) query.set('categoryId', categoryId)
    if (difficulty) query.set('difficulty', difficulty)
    if (teamSize) query.set('teamSize', teamSize)
    query.set('available', 'true')
    void loadProjects(`?${query.toString()}`)
  }

  return (
    <main className="mx-auto min-h-[65vh] max-w-6xl px-5 py-10 sm:px-8 sm:py-14">
      <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Published projects</p>
      <h1 className="mt-3 text-4xl font-black tracking-tight">Find your next collaboration</h1>
      <p className="mt-3 max-w-2xl text-slate-300">Search by topic, then narrow the catalog by skills, category, difficulty, or team size.</p>

      <form className="mt-8 grid gap-3 rounded-2xl border border-white/10 bg-white/[0.05] p-4 md:grid-cols-3" onSubmit={submit}>
        <input aria-label="Search projects" className={`${controlClass} md:col-span-2`} placeholder="Search title or description" value={search} onChange={(event) => setSearch(event.target.value)} />
        <select aria-label="Skill" className={controlClass} value={skillId} onChange={(event) => setSkillId(event.target.value)}><option value="">All skills</option>{skills.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <select aria-label="Category" className={controlClass} value={categoryId} onChange={(event) => setCategoryId(event.target.value)}><option value="">All categories</option>{categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <select aria-label="Difficulty" className={controlClass} value={difficulty} onChange={(event) => setDifficulty(event.target.value)}><option value="">All difficulties</option><option>Beginner</option><option>Intermediate</option><option>Advanced</option></select>
        <input aria-label="Team size" className={controlClass} min="1" max="20" placeholder="Team size" type="number" value={teamSize} onChange={(event) => setTeamSize(event.target.value)} />
        <button className="min-h-12 rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 hover:bg-cyan-300 md:col-span-3" type="submit">Apply filters</button>
      </form>

      {error && <p className="mt-6 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{error}</p>}
      {loading && <p className="mt-8 text-slate-300" role="status">Loading projects…</p>}
      {!loading && !error && projects.length === 0 && <p className="mt-8 rounded-2xl border border-white/10 p-6 text-slate-300">No published projects match these filters.</p>}
      <section className="mt-8 grid gap-5 md:grid-cols-2" aria-label="Project results">
        {projects.map((project) => (
          <article className="flex flex-col rounded-2xl border border-white/10 bg-white/[0.05] p-6" key={project.id}>
            <div className="flex flex-wrap gap-2 text-xs font-bold uppercase tracking-wider text-cyan-200"><span>{project.category.name}</span><span>•</span><span>{project.difficulty}</span></div>
            <h2 className="mt-3 text-2xl font-black">{project.title}</h2>
            <p className="mt-3 line-clamp-3 text-slate-300">{project.description}</p>
            <p className="mt-4 text-sm text-slate-400">Team: {project.minimumTeamSize}–{project.maximumTeamSize} · Preferred {project.preferredTeamSize}</p>
            <div className="mt-4 flex flex-wrap gap-2">{project.requiredSkills.map((skill) => <span className="rounded-full bg-slate-800 px-3 py-1 text-sm" key={skill.id}>{skill.name}</span>)}</div>
            <Link className="mt-6 inline-flex min-h-12 items-center justify-center rounded-xl border border-cyan-300/40 px-4 font-bold text-cyan-200 hover:bg-cyan-300/10" to={`/projects/${project.id}`}>View project</Link>
          </article>
        ))}
      </section>
    </main>
  )
}
