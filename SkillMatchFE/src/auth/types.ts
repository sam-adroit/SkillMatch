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
