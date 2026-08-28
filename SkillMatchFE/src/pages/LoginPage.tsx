import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../auth/api'
import { useAuth } from '../auth/useAuth'
import {
  AuthPageLayout,
  inputClassName,
  primaryButtonClassName,
} from '../components/AuthPageLayout'

export function LoginPage() {
  const { user, login } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) {
    return <Navigate to="/dashboard" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await login(email, password)
      const requestedPath = (location.state as { from?: string } | null)?.from
      navigate(requestedPath || '/dashboard', { replace: true })
    } catch (caughtError) {
      setError(
        caughtError instanceof ApiError
          ? caughtError.message
          : 'Unable to log in right now. Please try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthPageLayout
      eyebrow="Welcome back"
      title="Log in to SkillMatch"
      description="Use your Student or Admin account to continue."
    >
      <form className="grid gap-5" onSubmit={handleSubmit}>
        <div>
          <label className="font-semibold text-slate-200" htmlFor="login-email">
            Email address
          </label>
          <input
            autoComplete="email"
            className={inputClassName}
            id="login-email"
            name="email"
            placeholder="you@example.edu"
            required
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>
        <div>
          <label className="font-semibold text-slate-200" htmlFor="login-password">
            Password
          </label>
          <input
            autoComplete="current-password"
            className={inputClassName}
            id="login-password"
            minLength={8}
            name="password"
            required
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </div>

        {error && (
          <p className="rounded-xl border border-red-300/30 bg-red-400/10 px-4 py-3 text-sm text-red-200" role="alert">
            {error}
          </p>
        )}

        <button className={primaryButtonClassName} disabled={isSubmitting} type="submit">
          {isSubmitting ? 'Logging in…' : 'Log in'}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-slate-400">
        Need a Student account?{' '}
        <Link className="font-bold text-cyan-300 hover:text-cyan-200" to="/register">
          Create one
        </Link>
      </p>
    </AuthPageLayout>
  )
}
