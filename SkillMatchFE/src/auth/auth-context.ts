import { createContext } from 'react'
import type { CurrentUser } from './types'

export type AuthContextValue = {
  user: CurrentUser | null
  token: string | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
  authenticatedRequest: <T>(path: string, init?: RequestInit) => Promise<T>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
