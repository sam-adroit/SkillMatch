import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../auth/api'
import { useAuth } from '../auth/useAuth'
import {
  AuthPageLayout,
  inputClassName,
  primaryButtonClassName,
} from '../components/AuthPageLayout'

export function RegisterPage() {
  const { user, register } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) {
    return <Navigate to="/dashboard" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    if (password !== confirmation) {
      setError('Passwords must match.')
      return
    }

    setIsSubmitting(true)

    try {
      await register(email, password)
      navigate('/dashboard', { replace: true })
    } catch (caughtError) {
      setError(
        caughtError instanceof ApiError
          ? caughtError.message
          : 'Unable to create your account right now. Please try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthPageLayout
      eyebrow="Student registration"
      title="Create your account"
      description="Public registration always creates a Student account."
    >
      <form className="grid gap-5" onSubmit={handleSubmit}>
        <div>
          <label className="font-semibold text-slate-200" htmlFor="register-email">
            Email address
          </label>
          <input
            autoComplete="email"
            className={inputClassName}
            id="register-email"
            name="email"
            placeholder="you@example.edu"
            required
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>
        <div>
          <label className="font-semibold text-slate-200" htmlFor="register-password">
            Password
          </label>
          <input
            autoComplete="new-password"
            className={inputClassName}
            id="register-password"
            minLength={8}
            name="password"
            required
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          <p className="mt-2 text-sm text-slate-400">Use at least 8 characters.</p>
        </div>
        <div>
          <label className="font-semibold text-slate-200" htmlFor="register-confirmation">
            Confirm password
          </label>
          <input
            autoComplete="new-password"
            className={inputClassName}
            id="register-confirmation"
            minLength={8}
            name="confirmation"
            required
            type="password"
            value={confirmation}
            onChange={(event) => setConfirmation(event.target.value)}
          />
        </div>

        {error && (
          <p className="rounded-xl border border-red-300/30 bg-red-400/10 px-4 py-3 text-sm text-red-200" role="alert">
            {error}
          </p>
        )}

        <button className={primaryButtonClassName} disabled={isSubmitting} type="submit">
          {isSubmitting ? 'Creating account…' : 'Create Student account'}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-slate-400">
        Already registered?{' '}
        <Link className="font-bold text-cyan-300 hover:text-cyan-200" to="/login">
          Log in
        </Link>
      </p>
    </AuthPageLayout>
  )
}
