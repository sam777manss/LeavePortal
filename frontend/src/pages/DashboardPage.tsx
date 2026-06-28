import Navbar from '../components/Navbar'
import MyLeaves from '../components/MyLeaves'
import { useAuthStore } from '../store/authStore'
import ApplyLeave from '../components/ApplyLeave'

function DashboardPage() {
  const user = useAuthStore((state) => state.user)

  return (
    <>
      <Navbar />
      <div className="container mt-4">
        <h2>Welcome, {user?.fullName} 👋</h2>
        <p>Your role: {user?.role}</p>

        <h4 className="mt-4">Apply for Leave</h4>
        <ApplyLeave />

        <h4 className="mt-4">My Leave History</h4>
        <MyLeaves />
      </div>
    </>
  )
}

export default DashboardPage