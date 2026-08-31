import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AdminPage } from './pages/AdminPage'
import { DashboardPage } from './pages/DashboardPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProfilePage } from './pages/ProfilePage'
import { ProjectDetailPage } from './pages/ProjectDetailPage'
import { ProjectsPage } from './pages/ProjectsPage'
import { RegisterPage } from './pages/RegisterPage'
import { MyWorkPage } from './pages/MyWorkPage'
import { AdminWorkflowPage } from './pages/AdminWorkflowPage'
import { RecommendationsPage } from './pages/RecommendationsPage'
import { Toaster } from 'sonner'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Toaster closeButton duration={4000} position="top-right" richColors />
        <Routes>
          <Route element={<AppShell />}>
            <Route index element={<HomePage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="register" element={<RegisterPage />} />
            <Route element={<ProtectedRoute />}>
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="projects" element={<ProjectsPage />} />
              <Route path="projects/:projectId" element={<ProjectDetailPage />} />
            </Route>
            <Route element={<ProtectedRoute requiredRole="Student" />}>
              <Route path="profile" element={<ProfilePage />} />
              <Route path="my-work" element={<MyWorkPage />} />
              <Route path="recommendations" element={<RecommendationsPage />} />
            </Route>
            <Route element={<ProtectedRoute requiredRole="Admin" />}>
              <Route path="admin" element={<AdminPage />} />
              <Route path="admin/workflows" element={<AdminWorkflowPage />} />
            </Route>
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
