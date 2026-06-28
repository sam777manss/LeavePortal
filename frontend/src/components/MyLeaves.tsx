import { useQuery } from '@tanstack/react-query'
import api from '../api/axios'

// The shape of ONE leave row (matches the backend's LeaveApplicationDto).
type Leave = {
  id: number
  leaveTypeName: string
  startDate: string
  endDate: string
  totalDays: number
  reason: string
  status: string
  createdAt: string
}

function MyLeaves() {
  // useQuery = React Query's tool for READING data (a GET call).
  // Unlike useMutation, it runs AUTOMATICALLY when the component shows.
  const { data, isLoading, isError } = useQuery({
    queryKey: ['myLeaves'],            // a unique label for this data (used for caching)
    queryFn: async () => {
      const response = await api.get('/leave/my')
      return response.data as Leave[]  // backend returns an array of leaves
    },
  })

  if (isLoading) return <p>Loading your leaves...</p>
  if (isError) return <p className="text-danger">Failed to load leaves.</p>

  // No leaves yet
  if (!data || data.length === 0) {
    return <p>You haven't applied for any leave yet.</p>
  }

  return (
    <table className="table table-striped">
      <thead>
        <tr>
          <th>Type</th>
          <th>From</th>
          <th>To</th>
          <th>Days</th>
          <th>Status</th>
        </tr>
      </thead>
      <tbody>
        {/* .map loops over each leave and draws one <tr> row.
            key={leave.id} gives React a unique id per row (it needs this). */}
        {data.map((leave) => (
          <tr key={leave.id}>
            <td>{leave.leaveTypeName}</td>
            <td>{leave.startDate}</td>
            <td>{leave.endDate}</td>
            <td>{leave.totalDays}</td>
            <td>{leave.status}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default MyLeaves