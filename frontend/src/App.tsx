import { useEffect, useState } from 'react'
import { Routes, Route } from 'react-router-dom'
import api from './api/axios'
import { useAuthStore } from './store/authStore'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import ProtectedRoute from './components/ProtectedRoute'

function App() {
  debugger;
  const login = useAuthStore((state) => state.login)

  // "checking" is true while we ask the backend "who am I?".
  // We wait for the answer before showing pages (so refresh doesn't bounce us).
  const [checking, setChecking] = useState(true)

  // useEffect with [] runs ONCE, right after the app first loads (refresh included).
  useEffect(() => {
    // Ask the backend who is logged in. Browser auto-sends the cookie.
    api.get('/auth/me')
      .then((response) => {
        // Cookie was valid -> backend tells us the user -> put it back in the store.
        // /me returns { id, email, role, name }
        login({ fullName: response.data.name, role: response.data.role })
      })
      .catch(() => {
        // 401 = no valid cookie -> stay logged out, nothing to do.
      })
      .finally(() => {
        setChecking(false)  // done checking -> now we can show the app
      })
  }, [])

  // While we're still checking, show a simple loading message.
  if (checking) {
    return <p className="text-center mt-5">Loading...</p>
  }

  return (
    <Routes>
      <Route path="/" element={<LoginPage />} />
      <Route path="/dashboard" element={
        <ProtectedRoute>
          <DashboardPage />
        </ProtectedRoute>
      } />
    </Routes>
  )
}

export default App