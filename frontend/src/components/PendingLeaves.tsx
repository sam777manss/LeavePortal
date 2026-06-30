import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '../api/axios'

type Pending = {
  id: number
  employeeName: string
  employeeEmail: string
  leaveTypeName: string
  startDate: string
  endDate: string
  totalDays: number
  reason: string
  status: string
}

function PendingLeaves() {
  const queryClient = useQueryClient()  // used to refresh the list after an action

  const { data, isLoading, isError } = useQuery({
    queryKey: ['pendingLeaves'],
    queryFn: async () => {
      const response = await api.get('/leave/pending')
      return response.data as Pending[]
    },
  })

  // APPROVE mutation.
  // Note: mutationFn here takes an argument (vars). Whatever we pass to
  // mutate(...) below shows up here as `vars`.
  const approveMutation = useMutation({
    mutationFn: async (vars: { id: number; comment: string }) => {
      await api.put(`/leave/${vars.id}/approve`, { comment: vars.comment })
    },
    onSuccess: () => {
      // Refresh the pending list -> the approved row disappears automatically.
      queryClient.invalidateQueries({ queryKey: ['pendingLeaves'] })
    },
  })

  // REJECT mutation (same idea).
  const rejectMutation = useMutation({
    mutationFn: async (vars: { id: number; comment: string }) => {
      await api.put(`/leave/${vars.id}/reject`, { comment: vars.comment })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pendingLeaves'] })
    },
  })

  // Called when the Approve button is clicked for a row.
  function handleApprove(id: number) {
    const comment = window.prompt('Approval comment (optional):') || ''
    approveMutation.mutate({ id, comment })   // <-- this value becomes `vars` above
  }

  // Called when the Reject button is clicked for a row.
  function handleReject(id: number) {
    const comment = window.prompt('Reason for rejection (required):')
    // Reject needs a comment — the backend requires it too.
    if (!comment || comment.trim() === '') {
      alert('A comment is required to reject.')
      return
    }
    rejectMutation.mutate({ id, comment })
  }

  if (isLoading) return <p>Loading pending requests...</p>
  if (isError) return <p className="text-danger">Failed to load pending requests.</p>
  if (!data || data.length === 0) return <p>No pending requests. 🎉</p>

  return (
    <table className="table table-striped align-middle">
      <thead>
        <tr>
          <th>Employee</th>
          <th>Type</th>
          <th>From</th>
          <th>To</th>
          <th>Days</th>
          <th>Reason</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {data.map((leave) => (
          <tr key={leave.id}>
            <td>{leave.employeeName}</td>
            <td>{leave.leaveTypeName}</td>
            <td>{leave.startDate}</td>
            <td>{leave.endDate}</td>
            <td>{leave.totalDays}</td>
            <td>{leave.reason}</td>
            <td>
              <button
                className="btn btn-success btn-sm me-2"
                onClick={() => handleApprove(leave.id)}
              >
                Approve
              </button>
              <button
                className="btn btn-danger btn-sm"
                onClick={() => handleReject(leave.id)}
              >
                Reject
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default PendingLeaves