import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import api from '../api/axios'
import { useAuthStore } from '../store/authStore'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const navigate = useNavigate()                      // lets us send the user to another page
  const login = useAuthStore((state) => state.login)  // store action to save the logged-in user

  // useMutation = React Query's tool for an action call (login).
  // It gives us loading + error status for free.
  const loginMutation = useMutation({
    // mutationFn = the actual API call. What we return becomes "data" in onSuccess.
    mutationFn: async () => {
      debugger;
      const response = await api.post('/auth/login', { email, password })
      return response.data   // backend returns { id, fullName, email, role }
    },
    // Runs only if the call succeeded (the cookie is now set by the browser).
    onSuccess: (data) => {
      login({ fullName: data.fullName, role: data.role })
      // Managers go to the manager dashboard, everyone else to the normal one.
      if (data.role === 'Manager') {
        navigate('/manager')
      } else {
        navigate('/dashboard')
      }
    },
  })

  function handleSubmit(event: React.FormEvent) {
    debugger;
    event.preventDefault()
    loginMutation.mutate()   // start the login call
  }

  return (
    <div className="container d-flex justify-content-center align-items-center" style={{ minHeight: '100vh' }}>
      <div className="card shadow p-4" style={{ width: '380px' }}>
        <h3 className="text-center mb-4">LeavePortal Login</h3>

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label">Email</label>
            <input
              type="email"
              className="form-control"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="mb-3">
            <label className="form-label">Password</label>
            <input
              type="password"
              className="form-control"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          {/* Show an error message if login failed */}
          {loginMutation.isError && (
            <div className="alert alert-danger py-2">
              Invalid email or password.
            </div>
          )}

          {/* Button disabled + shows text while the call runs */}
          <button type="submit" className="btn btn-primary w-100" disabled={loginMutation.isPending}>
            {loginMutation.isPending ? 'Logging in...' : 'Login'}
          </button>
        </form>
      </div>
    </div>
  )
}

export default LoginPage