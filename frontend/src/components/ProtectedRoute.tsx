import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

type Props = {
  children: React.ReactNode
  role?: string   // optional: if set, the user MUST have this role to see the page
}

function ProtectedRoute({ children, role }: Props) {
  debugger;
  const isLoggedIn = useAuthStore((state) => state.isLoggedIn)
  const user = useAuthStore((state) => state.user)

  // Not logged in -> go to login.
  if (!isLoggedIn) {
    return <Navigate to="/" replace />
  }

  // A role is required but the user doesn't have it -> send them to the normal dashboard.
  if (role && user?.role !== role) {
    return <Navigate to="/dashboard" replace />
  }

  return <>{children}</>
} 

export default ProtectedRoute 