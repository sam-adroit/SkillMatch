import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../auth/api'
import type { AdminProject, Lookup } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { Link } from 'react-router-dom'

type LookupKind = 'skills' | 'interests' | 'categories'
const inputClass = 'min-h-12 w-full rounded-xl border border-white/15 bg-slate-950 px-4 py-3 text-white'
const emptyForm = { title: '', description: '', adminNotes: '', difficulty: 'Beginner', categoryId: '', minimumTeamSize: 2, preferredTeamSize: 3, maximumTeamSize: 4, requiredSkillIds: [] as string[] }

export function AdminPage() {
  const { authenticatedRequest } = useAuth()
  const [lookups, setLookups] = useState<Record<LookupKind, Lookup[]>>({ skills: [], interests: [], categories: [] })
  const [projects, setProjects] = useState<AdminProject[]>([])
  const [lookupNames, setLookupNames] = useState<Record<LookupKind, string>>({ skills: '', interests: '', categories: '' })
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const [skills, interests, categories, loadedProjects] = await Promise.all([
        authenticatedRequest<Lookup[]>('/api/skills'), authenticatedRequest<Lookup[]>('/api/interests'),
        authenticatedRequest<Lookup[]>('/api/categories'), authenticatedRequest<AdminProject[]>('/api/admin/projects'),
      ])
      setLookups({ skills, interests, categories }); setProjects(loadedProjects)
      setForm((current) => current.categoryId || categories.length === 0 ? current : { ...current, categoryId: categories[0].id })
    } catch { setError('Unable to load the admin workspace.') }
  }, [authenticatedRequest])

  useEffect(() => { void load() }, [load])

  function showError(caught: unknown, fallback: string) {
    setMessage(null); setError(caught instanceof ApiError ? caught.message : fallback)
  }

  async function createLookup(kind: LookupKind, event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(null); setMessage(null)
    try {
      await authenticatedRequest<Lookup>(`/api/admin/${kind}`, { method: 'POST', body: JSON.stringify({ name: lookupNames[kind] }) })
      setLookupNames((current) => ({ ...current, [kind]: '' })); setMessage('Lookup added.'); await load()
    } catch (caught) { showError(caught, 'Unable to add the lookup.') }
  }

  async function renameLookup(kind: LookupKind, item: Lookup) {
    const name = window.prompt(`Rename ${item.name}`, item.name)
    if (!name || name.trim() === item.name) return
    try { await authenticatedRequest(`/api/admin/${kind}/${item.id}`, { method: 'PUT', body: JSON.stringify({ name }) }); setMessage('Lookup renamed.'); await load() }
    catch (caught) { showError(caught, 'Unable to rename the lookup.') }
  }

  async function deleteLookup(kind: LookupKind, item: Lookup) {
    if (!window.confirm(`Delete ${item.name}?`)) return
    try { await authenticatedRequest(`/api/admin/${kind}/${item.id}`, { method: 'DELETE' }); setMessage('Lookup deleted.'); await load() }
    catch (caught) { showError(caught, 'Unable to delete the lookup.') }
  }

  function toggleSkill(id: string) {
    setForm((current) => ({ ...current, requiredSkillIds: current.requiredSkillIds.includes(id) ? current.requiredSkillIds.filter((value) => value !== id) : [...current.requiredSkillIds, id] }))
  }

  async function saveProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(null); setMessage(null)
    try {
      await authenticatedRequest(editingId ? `/api/admin/projects/${editingId}` : '/api/admin/projects', { method: editingId ? 'PUT' : 'POST', body: JSON.stringify(form) })
      setMessage(editingId ? 'Project updated.' : 'Draft project created.'); setEditingId(null); setForm({ ...emptyForm, categoryId: lookups.categories[0]?.id ?? '' }); await load()
    } catch (caught) { showError(caught, 'Unable to save the project.') }
  }

  function editProject(project: AdminProject) {
    setEditingId(project.id)
    setForm({ title: project.title, description: project.description, adminNotes: project.adminNotes, difficulty: project.difficulty, categoryId: project.category.id, minimumTeamSize: project.minimumTeamSize, preferredTeamSize: project.preferredTeamSize, maximumTeamSize: project.maximumTeamSize, requiredSkillIds: project.requiredSkills.map((item) => item.id) })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  async function statusProject(id: string, status: 'Published' | 'Closed') {
    try { await authenticatedRequest(`/api/admin/projects/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }); setMessage(`Project ${status.toLowerCase()}.`); await load() }
    catch (caught) { showError(caught, `Unable to mark the project ${status.toLowerCase()}.`) }
  }

  async function deleteProject(id: string) {
    if (!window.confirm('Delete this draft project?')) return
    try { await authenticatedRequest(`/api/admin/projects/${id}`, { method: 'DELETE' }); setMessage('Draft project deleted.'); await load() }
    catch (caught) { showError(caught, 'Unable to delete the project.') }
  }

  return (
    <main className="mx-auto min-h-[65vh] max-w-6xl px-5 py-10 sm:px-8 sm:py-14">
      <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Admin only</p><h1 className="mt-3 text-4xl font-black tracking-tight">Catalog and project workspace</h1>
      <Link className="mt-6 inline-flex min-h-12 items-center rounded-xl bg-violet-400 px-5 font-bold text-slate-950 hover:bg-violet-300" to="/admin/workflows">Open applications, teams, and dashboard</Link>
      {error && <p className="mt-6 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{error}</p>}
      {message && <p className="mt-6 rounded-xl border border-emerald-300/30 bg-emerald-400/10 p-4 text-emerald-200" role="status">{message}</p>}

      <section className="mt-8 rounded-2xl border border-white/10 bg-white/[0.05] p-5 sm:p-8"><h2 className="text-2xl font-black">{editingId ? 'Edit project' : 'Create a draft project'}</h2>
        <form className="mt-5 grid gap-4 md:grid-cols-2" onSubmit={saveProject}>
          <label className="font-semibold md:col-span-2">Title<input className={`${inputClass} mt-2`} minLength={3} required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label>
          <label className="font-semibold md:col-span-2">Description<textarea className={`${inputClass} mt-2 min-h-32`} minLength={20} required value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
          <label className="font-semibold md:col-span-2">Private admin notes<textarea className={`${inputClass} mt-2 min-h-24`} value={form.adminNotes} onChange={(e) => setForm({ ...form, adminNotes: e.target.value })} /></label>
          <label className="font-semibold">Difficulty<select className={`${inputClass} mt-2`} value={form.difficulty} onChange={(e) => setForm({ ...form, difficulty: e.target.value })}><option>Beginner</option><option>Intermediate</option><option>Advanced</option></select></label>
          <label className="font-semibold">Category<select className={`${inputClass} mt-2`} required value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}><option value="">Choose a category</option>{lookups.categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
          {(['minimumTeamSize', 'preferredTeamSize', 'maximumTeamSize'] as const).map((field) => <label className="font-semibold" key={field}>{field.replace('TeamSize', ' team size')}<input className={`${inputClass} mt-2`} min="1" max="20" required type="number" value={form[field]} onChange={(e) => setForm({ ...form, [field]: Number(e.target.value) })} /></label>)}
          <fieldset className="md:col-span-2"><legend className="font-semibold">Required skills</legend><div className="mt-3 grid gap-2 sm:grid-cols-2 md:grid-cols-3">{lookups.skills.map((item) => <label className="flex min-h-12 items-center gap-3 rounded-xl border border-white/10 bg-slate-950 px-4" key={item.id}><input checked={form.requiredSkillIds.includes(item.id)} type="checkbox" onChange={() => toggleSkill(item.id)} />{item.name}</label>)}</div></fieldset>
          <button className="min-h-12 rounded-xl bg-cyan-400 px-5 font-bold text-slate-950 md:col-span-2" type="submit">{editingId ? 'Save changes' : 'Create draft'}</button>
          {editingId && <button className="min-h-12 rounded-xl border border-white/20 px-5 font-bold md:col-span-2" type="button" onClick={() => { setEditingId(null); setForm({ ...emptyForm, categoryId: lookups.categories[0]?.id ?? '' }) }}>Cancel editing</button>}
        </form>
      </section>

      <section className="mt-8"><h2 className="text-2xl font-black">Projects</h2>{projects.length === 0 && <p className="mt-4 rounded-xl border border-white/10 p-5 text-slate-300">No projects yet. Create the first draft above.</p>}<div className="mt-4 grid gap-4">
        {projects.map((project) => <article className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={project.id}><div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between"><div><p className="text-sm font-bold uppercase tracking-wider text-cyan-300">{project.status} · {project.category.name}</p><h3 className="mt-1 text-xl font-black">{project.title}</h3></div><div className="flex flex-wrap gap-2"><button className="min-h-11 rounded-lg border border-white/20 px-4" onClick={() => editProject(project)}>Edit</button>{project.status === 'Draft' && <button className="min-h-11 rounded-lg bg-emerald-400 px-4 font-bold text-slate-950" onClick={() => void statusProject(project.id, 'Published')}>Publish</button>}{project.status === 'Published' && <button className="min-h-11 rounded-lg bg-amber-300 px-4 font-bold text-slate-950" onClick={() => void statusProject(project.id, 'Closed')}>Close</button>}{project.status === 'Draft' && <button className="min-h-11 rounded-lg border border-red-300/40 px-4 text-red-200" onClick={() => void deleteProject(project.id)}>Delete</button>}</div></div></article>)}
      </div></section>

      <section className="mt-10"><h2 className="text-2xl font-black">Lookup catalogs</h2><div className="mt-4 grid gap-5 lg:grid-cols-3">
        {(['skills', 'interests', 'categories'] as LookupKind[]).map((kind) => <div className="rounded-2xl border border-white/10 bg-white/[0.05] p-5" key={kind}><h3 className="text-xl font-black capitalize">{kind}</h3><form className="mt-4 flex gap-2" onSubmit={(event) => void createLookup(kind, event)}><input aria-label={`New ${kind}`} className={inputClass} minLength={2} required value={lookupNames[kind]} onChange={(e) => setLookupNames({ ...lookupNames, [kind]: e.target.value })} /><button className="rounded-xl bg-cyan-400 px-4 font-bold text-slate-950">Add</button></form><ul className="mt-4 grid gap-2">{lookups[kind].map((item) => <li className="flex min-h-12 items-center justify-between gap-2 rounded-xl bg-slate-950 px-3" key={item.id}><span>{item.name}</span><span><button className="p-2 text-cyan-200" onClick={() => void renameLookup(kind, item)}>Rename</button><button className="p-2 text-red-200" onClick={() => void deleteLookup(kind, item)}>Delete</button></span></li>)}</ul></div>)}
      </div></section>
    </main>
  )
}
