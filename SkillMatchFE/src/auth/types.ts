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

export type ProjectApplication = {
  id: string
  studentId: string
  studentEmail: string
  projectId: string
  projectTitle: string
  note: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Waitlisted'
  appliedAt: string
  decidedAt: string | null
  decisionNote: string
}

export type TeamMember = { studentId: string; email: string; isLeader: boolean; joinedAt: string }

export type Team = {
  id: string
  projectId: string
  projectTitle: string
  name: string
  status: string
  maximumSize: number
  members: TeamMember[]
  createdAt: string
  updatedAt: string
}

export type AdminDashboard = {
  students: number
  projects: number
  teams: number
  pendingApplications: number
  unassignedStudents: number
}
