export type UserRole = 'Student' | 'Admin'

export type CurrentUser = {
  id: string
  email: string
  role: UserRole
}

export type AuthResponse = {
  token: string
  expiresAt: string
  user: CurrentUser
}

export type AdminAccessResponse = {
  message: string
  user: CurrentUser
}

export type Lookup = { id: string; name: string }

export type StudentProfile = {
  userId: string
  email: string
  experienceLevel: string
  goals: string
  preferredTechnologies: string[]
  skills: Lookup[]
  interests: Lookup[]
  completenessPercent: number
  missingFields: string[]
  updatedAt: string | null
}

export type Project = {
  id: string
  title: string
  description: string
  difficulty: string
  status: string
  minimumTeamSize: number
  preferredTeamSize: number
  maximumTeamSize: number
  category: Lookup
  requiredSkills: Lookup[]
  createdAt: string
  updatedAt: string
}

export type AdminProject = Project & { adminNotes: string }
