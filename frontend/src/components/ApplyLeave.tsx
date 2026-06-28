import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '../api/axios'

// Shape of one leave type (from GET /api/leave/types)
type LeaveType = {
  id: number
  name: string
  defaultDays: number
}

function ApplyLeave() {
  const queryClient = useQueryClient()  // used to refresh the leave table after applying

  // Form state — one piece per field.
  const [leaveTypeId, setLeaveTypeId] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [reason, setReason] = useState('')
  const [file, setFile] = useState<File | null>(null)

  // Load the leave types for the dropdown (runs automatically).
  const { data: leaveTypes } = useQuery({
    queryKey: ['leaveTypes'],
    queryFn: async () => {
      const response = await api.get('/leave/types')
      return response.data as LeaveType[]
    },
  })

  // Submit a new leave application.
  const applyMutation = useMutation({
    mutationFn: async () => {
        debugger;
      // The backend expects multipart/form-data ([FromForm] + optional file),
      // so we build a FormData object instead of a plain JSON body.
      const formData = new FormData()
      formData.append('LeaveTypeId', leaveTypeId)
      formData.append('StartDate', startDate)
      formData.append('EndDate', endDate)
      formData.append('Reason', reason)
      if (file) {
        formData.append('document', file)  // 'document' matches the IFormFile name
      }

      const response = await api.post('/leave/apply', formData)
      return response.data
    },
    onSuccess: () => {
        debugger;
      // Tell React Query the 'myLeaves' data is now stale -> it refetches ->
      // the table below updates instantly with the new row. (No manual reload!)
      queryClient.invalidateQueries({ queryKey: ['myLeaves'] })

      // Clear the form.
      setLeaveTypeId('')
      setStartDate('')
      setEndDate('')
      setReason('')
      setFile(null)
    },
  })

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    applyMutation.mutate()
  }

  return (
    <form onSubmit={handleSubmit} className="card p-3 mb-4">
      <h5 className="mb-3">Apply for Leave</h5>

      {/* Leave type dropdown — options come from the API */}
      <div className="mb-2">
        <label className="form-label">Leave Type</label>
        <select
          className="form-select"
          value={leaveTypeId}
          onChange={(e) => setLeaveTypeId(e.target.value)}
          required
        >
          <option value="">-- Select --</option>
          {leaveTypes?.map((type) => (
            <option key={type.id} value={type.id}>
              {type.name}
            </option>
          ))}
        </select>
      </div>

      <div className="row">
        <div className="col mb-2">
          <label className="form-label">Start Date</label>
          <input
            type="date"
            className="form-control"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            required
          />
        </div>
        <div className="col mb-2">
          <label className="form-label">End Date</label>
          <input
            type="date"
            className="form-control"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            required
          />
        </div>
      </div>

      <div className="mb-2">
        <label className="form-label">Reason</label>
        <textarea
          className="form-control"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          required
        />
      </div>

      <div className="mb-3">
        <label className="form-label">Document (optional)</label>
        <input
          type="file"
          className="form-control"
          // e.target.files is a list; we take the first file (or null)
          onChange={(e) => setFile(e.target.files ? e.target.files[0] : null)}
        />
      </div>

      {applyMutation.isError && (
        <div className="alert alert-danger py-2">Failed to apply. Check the dates and try again.</div>
      )}
      {applyMutation.isSuccess && (
        <div className="alert alert-success py-2">Leave applied successfully! ✅</div>
      )}

      <button type="submit" className="btn btn-primary" disabled={applyMutation.isPending}>
        {applyMutation.isPending ? 'Submitting...' : 'Apply'}
      </button>
    </form>
  )
}

export default ApplyLeave