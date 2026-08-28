import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { UserRole } from '../auth/types'
import { useAuth } from '../auth/useAuth'

export function ProtectedRoute({ requiredRole }: { requiredRole?: UserRole }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <main className="mx-auto grid min-h-[60vh] max-w-6xl place-items-center px-5 sm:px-8">
        <p className="text-slate-300" role="status">
          Restoring your session…
        </p>
      </main>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (requiredRole && user.role !== requiredRole) {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}
