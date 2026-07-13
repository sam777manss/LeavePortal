import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import api from '../api/axios'
import { useAuthStore } from '../store/authStore'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const navigate = useNavigate()
  const login = useAuthStore((state) => state.login)

  const loginMutation = useMutation({
    mutationFn: async () => {
      const response = await api.post('/auth/login', { email, password })
      return response.data
    },
    onSuccess: (data) => {
      login({ id: data.id, fullName: data.fullName, role: data.role })
      if (data.role === 'Manager') {
        navigate('/manager')
      } else {
        navigate('/dashboard')
      }
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    loginMutation.mutate()
  }

  return (
    <div className="login-page">
      <div className="login-container">
        <h1>LOGIN 🔐</h1>

        <form onSubmit={handleSubmit}>
          {/* use login-field, NOT Bootstrap's input-group */}
          <div className="login-field">
            <label>EMAIL</label>
            <input
              type="email"
              className="form-control"
              placeholder="your@email.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="login-field">
            <label>PASSWORD</label>
            <input
              type="password"
              className="form-control"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          {loginMutation.isError && (
            <div className="alert alert-danger py-2">Invalid email or password.</div>
          )}

          <button type="submit" className="btn btn-primary w-100" disabled={loginMutation.isPending}>
            {loginMutation.isPending ? 'SIGNING IN...' : 'SIGN IN'}
          </button>
        </form>
      </div>
    </div>
  )
}

export default LoginPage