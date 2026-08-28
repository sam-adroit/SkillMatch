import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { apiRequest } from './api'
import { AuthContext, type AuthContextValue } from './auth-context'
import type { AuthResponse, CurrentUser } from './types'

const tokenStorageKey = 'skillmatch.auth.token'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem(tokenStorageKey),
  )
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const clearSession = useCallback(() => {
    localStorage.removeItem(tokenStorageKey)
    setToken(null)
    setUser(null)
  }, [])

  const saveSession = useCallback((response: AuthResponse) => {
    localStorage.setItem(tokenStorageKey, response.token)
    setToken(response.token)
    setUser(response.user)
  }, [])

  useEffect(() => {
    let isActive = true

    async function restoreSession() {
      if (!token) {
        setIsLoading(false)
        return
      }

      try {
        const currentUser = await apiRequest<CurrentUser>('/api/auth/me', {}, token)

        if (isActive) {
          setUser(currentUser)
        }
      } catch {
        if (isActive) {
          clearSession()
        }
      } finally {
        if (isActive) {
          setIsLoading(false)
        }
      }
    }

    void restoreSession()

    return () => {
      isActive = false
    }
  }, [clearSession, token])

  const login = useCallback(
    async (email: string, password: string) => {
      const response = await apiRequest<AuthResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })
      saveSession(response)
    },
    [saveSession],
  )

  const register = useCallback(
    async (email: string, password: string) => {
      const response = await apiRequest<AuthResponse>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })
      saveSession(response)
    },
    [saveSession],
  )

  const authenticatedRequest = useCallback(
    async <T,>(path: string, init: RequestInit = {}) => {
      if (!token) {
        throw new Error('You must be signed in to continue.')
      }

      return apiRequest<T>(path, init, token)
    },
    [token],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isLoading,
      login,
      register,
      logout: clearSession,
      authenticatedRequest,
    }),
    [authenticatedRequest, clearSession, isLoading, login, register, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
