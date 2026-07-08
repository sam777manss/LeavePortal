import { Link, useNavigate } from 'react-router-dom'
import api from '../api/axios'
import { useAuthStore } from '../store/authStore'

function Navbar() {
    debugger;
  const navigate = useNavigate()
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)  // store action to clear user

  async function handleLogout() {
    await api.post('/auth/logout')   // ask backend to delete the cookie
    logout()                         // clear the Zustand store (name/role)
    navigate('/')                    // send user back to the login page
  }

  return (
    <nav className="navbar navbar-dark bg-primary px-4">
      <span className="navbar-brand">LeavePortal</span>

      <div className="d-flex align-items-center gap-3">
        <Link to="/chat" className="text-white text-decoration-none">Chat</Link>
        {/* show who is logged in */}
        <span className="text-white">{user?.fullName} ({user?.role})</span>
        <button className="btn btn-light btn-sm" onClick={handleLogout}>
          Logout
        </button>
      </div>
    </nav>
  )
}

export default Navbar