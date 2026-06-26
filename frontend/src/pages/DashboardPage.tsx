import { useAuthStore } from '../store/authStore'

function DashboardPage() {
  const user = useAuthStore((state) => state.user)

  return (
    <div className="container mt-5">
      {/* user?.fullName -> the "?" means "only if user is not null" (it could be null) */}
      <h2>Welcome, {user?.fullName} 👋</h2>
      <p>Your role: {user?.role}</p>
    </div>
  )
}

export default DashboardPage