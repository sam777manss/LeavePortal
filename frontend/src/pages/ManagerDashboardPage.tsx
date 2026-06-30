import Navbar from '../components/Navbar'
import PendingLeaves from '../components/PendingLeaves'
import { useAuthStore } from '../store/authStore'

function ManagerDashboardPage() {
    debugger;
  const user = useAuthStore((state) => state.user)

  return (
    <>
      <Navbar />
      <div className="container mt-4">
        <h2>Manager Dashboard</h2>
        <p>Welcome, {user?.fullName} ({user?.role})</p>

        <h4 className="mt-4">Pending Requests</h4>
        <PendingLeaves />
      </div>
    </>
  )
}

export default ManagerDashboardPage