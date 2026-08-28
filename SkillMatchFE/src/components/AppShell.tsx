import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `block rounded-lg px-3 py-2 transition ${
    isActive ? 'bg-cyan-400 text-slate-950' : 'text-slate-300 hover:bg-white/10'
  }`

export function AppShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-white/10 bg-slate-950/95">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-5 py-4 sm:px-8">
          <Link className="flex items-center gap-3 rounded-md" to="/">
            <span
              className="grid size-10 place-items-center rounded-xl bg-cyan-400 font-black text-slate-950"
              aria-hidden="true"
            >
              SM
            </span>
            <span>
              <span className="block text-base font-bold tracking-tight">
                SkillMatch AI
              </span>
              <span className="block text-xs text-slate-400">
                Teams built around potential
              </span>
            </span>
          </Link>

          <nav aria-label="Primary navigation" className="w-full sm:w-auto">
            <ul className="flex flex-wrap items-center gap-1 text-sm font-semibold">
              <li>
                <NavLink className={navLinkClass} to="/" end>
                  Home
                </NavLink>
              </li>
              {user ? (
                <>
                  <li>
                    <NavLink className={navLinkClass} to="/dashboard">
                      Dashboard
                    </NavLink>
                  </li>
                  {user.role === 'Admin' && (
                    <li>
                      <NavLink className={navLinkClass} to="/admin">
                        Admin
                      </NavLink>
                    </li>
                  )}
                  <li>
                    <button
                      className="min-h-10 rounded-lg px-3 py-2 text-slate-300 hover:bg-white/10"
                      type="button"
                      onClick={handleLogout}
                    >
                      Log out
                    </button>
                  </li>
                </>
              ) : (
                <>
                  <li>
                    <NavLink className={navLinkClass} to="/login">
                      Log in
                    </NavLink>
                  </li>
                  <li>
                    <NavLink className={navLinkClass} to="/register">
                      Create account
                    </NavLink>
                  </li>
                </>
              )}
            </ul>
          </nav>
        </div>
      </header>

      <Outlet />

      <footer className="mx-auto flex max-w-6xl flex-col gap-2 px-5 py-8 text-sm text-slate-400 sm:flex-row sm:items-center sm:justify-between sm:px-8">
        <p>SkillMatch AI</p>
        <p>Built for better project teams.</p>
      </footer>
    </div>
  )
}
