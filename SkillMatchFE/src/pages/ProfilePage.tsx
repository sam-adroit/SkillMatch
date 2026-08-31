import { useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../auth/api'
import type { Lookup, StudentProfile } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { toast } from 'sonner'

const inputClass = 'mt-2 min-h-12 w-full rounded-xl border border-white/15 bg-slate-950 px-4 py-3 text-white'

export function ProfilePage() {
  const { authenticatedRequest } = useAuth()
  const [profile, setProfile] = useState<StudentProfile | null>(null)
  const [skills, setSkills] = useState<Lookup[]>([])
  const [interests, setInterests] = useState<Lookup[]>([])
  const [level, setLevel] = useState('Beginner')
  const [goals, setGoals] = useState('')
  const [technologies, setTechnologies] = useState('')
  const [skillIds, setSkillIds] = useState<string[]>([])
  const [interestIds, setInterestIds] = useState<string[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([
      authenticatedRequest<StudentProfile>('/api/profile'),
      authenticatedRequest<Lookup[]>('/api/skills'),
      authenticatedRequest<Lookup[]>('/api/interests'),
    ]).then(([loadedProfile, loadedSkills, loadedInterests]) => {
      setProfile(loadedProfile)
      setSkills(loadedSkills)
      setInterests(loadedInterests)
      setLevel(loadedProfile.experienceLevel || 'Beginner')
      setGoals(loadedProfile.goals)
      setTechnologies(loadedProfile.preferredTechnologies.join(', '))
      setSkillIds(loadedProfile.skills.map((item) => item.id))
      setInterestIds(loadedProfile.interests.map((item) => item.id))
    }).catch(() => setLoadError('Unable to load your profile.'))
  }, [authenticatedRequest])

  function toggle(id: string, values: string[], setter: (value: string[]) => void) {
    setter(values.includes(id) ? values.filter((value) => value !== id) : [...values, id])
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      const updated = await authenticatedRequest<StudentProfile>('/api/profile', {
        method: 'PUT',
        body: JSON.stringify({
          experienceLevel: level,
          goals,
          preferredTechnologies: technologies.split(',').map((item) => item.trim()).filter(Boolean),
          skillIds,
          interestIds,
        }),
      })
      setProfile(updated)
      toast.success('Profile saved successfully.')
    } catch (caught) {
      toast.error(caught instanceof ApiError ? caught.message : 'Unable to save your profile.')
    }
  }

  return (
    <main className="mx-auto min-h-[65vh] max-w-4xl px-5 py-10 sm:px-8 sm:py-14">
      <p className="text-sm font-bold uppercase tracking-[0.18em] text-cyan-300">Student profile</p>
      <div className="mt-3 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div><h1 className="text-4xl font-black tracking-tight">{profile ? `${profile.firstName} ${profile.lastName}` : 'Show what you bring'}</h1><p className="mt-3 text-slate-300">Skills and interests power later project recommendations.</p></div>
        <div className="rounded-xl border border-cyan-300/30 bg-cyan-300/10 px-4 py-3 font-bold text-cyan-200">
          {profile?.completenessPercent ?? 0}% complete
        </div>
      </div>
      {profile && profile.missingFields.length > 0 && (
        <p className="mt-6 rounded-xl border border-amber-300/30 bg-amber-300/10 p-4 text-amber-100" role="status">
          Complete: {profile.missingFields.join(', ')}.
        </p>
      )}
      {loadError && <p className="mt-6 rounded-xl border border-red-300/30 bg-red-400/10 p-4 text-red-200" role="alert">{loadError}</p>}
      <form className="mt-8 grid gap-7 rounded-2xl border border-white/10 bg-white/[0.05] p-5 sm:p-8" onSubmit={handleSubmit}>
        <label className="font-semibold">Experience level
          <select className={inputClass} value={level} onChange={(event) => setLevel(event.target.value)}>
            <option>Beginner</option><option>Intermediate</option><option>Advanced</option>
          </select>
        </label>
        <label className="font-semibold">Goals
          <textarea className={`${inputClass} min-h-32`} minLength={10} required value={goals} onChange={(event) => setGoals(event.target.value)} />
        </label>
        <label className="font-semibold">Preferred technologies
          <input className={inputClass} placeholder="React, PostgreSQL, C#" required value={technologies} onChange={(event) => setTechnologies(event.target.value)} />
          <span className="mt-2 block text-sm font-normal text-slate-400">Separate technologies with commas.</span>
        </label>
        <fieldset><legend className="font-semibold">Skills</legend><div className="mt-3 grid gap-3 sm:grid-cols-2">
          {skills.map((item) => <label className="flex min-h-12 items-center gap-3 rounded-xl border border-white/10 bg-slate-950 px-4" key={item.id}><input checked={skillIds.includes(item.id)} type="checkbox" onChange={() => toggle(item.id, skillIds, setSkillIds)} />{item.name}</label>)}
        </div></fieldset>
        <fieldset><legend className="font-semibold">Interests</legend><div className="mt-3 grid gap-3 sm:grid-cols-2">
          {interests.map((item) => <label className="flex min-h-12 items-center gap-3 rounded-xl border border-white/10 bg-slate-950 px-4" key={item.id}><input checked={interestIds.includes(item.id)} type="checkbox" onChange={() => toggle(item.id, interestIds, setInterestIds)} />{item.name}</label>)}
        </div></fieldset>
        <button className="min-h-12 rounded-xl bg-cyan-400 px-5 py-3 font-bold text-slate-950 hover:bg-cyan-300" type="submit">Save profile</button>
      </form>
    </main>
  )
}
