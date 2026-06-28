import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

// "children" = whatever page we wrap inside this guard (e.g. <DashboardPage />).
type Props = {
  children: React.ReactNode   // React.ReactNode = "any JSX/content"
}

function ProtectedRoute({ children }: Props) {
    const isLoggedIn = useAuthStore((state) => state.isLoggedIn)
    debugger;

  // Not logged in? Redirect to the login page instead of showing the page.
  // <Navigate> just sends the user to another route. "replace" = don't keep
  // the blocked page in browser history (so Back won't return to it).
  if (!isLoggedIn) {
    return <Navigate to="/" replace />
  }

  // Logged in -> show the wrapped page as normal.
  return <>{children}</>
}

export default ProtectedRoute