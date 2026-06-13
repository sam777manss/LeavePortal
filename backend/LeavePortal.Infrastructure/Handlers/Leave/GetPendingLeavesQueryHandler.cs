using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.Enums;
using LeavePortal.Core.Queries.Leave;
using LeavePortal.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Leave;

public class GetPendingLeavesQueryHandler : IRequestHandler<GetPendingLeavesQuery, List<PendingLeaveDto>>
{
    private readonly LeavePortalDbContext _context;

    public GetPendingLeavesQueryHandler(LeavePortalDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingLeaveDto>> Handle(GetPendingLeavesQuery request, CancellationToken cancellationToken)
    {
        // Read-only query → AsNoTracking() (no change tracking needed).
        // Oldest first (ascending CreatedAt): a manager's review queue works FIFO so the
        // longest-waiting request surfaces at the top. We project employee + leave-type
        // names straight into the DTO in SQL, so only the needed columns are fetched.
        return await _context.LeaveApplications
            .AsNoTracking()
            .Where(a => a.Status == LeaveStatus.Pending.ToString())
            .OrderBy(a => a.CreatedAt)
            .Select(a => new PendingLeaveDto
            {
                Id = a.Id,
                UserId = a.UserId,
                EmployeeName = a.User.FullName,
                EmployeeEmail = a.User.Email,
                LeaveTypeId = a.LeaveTypeId,
                LeaveTypeName = a.LeaveType.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                TotalDays = a.TotalDays,
                Reason = a.Reason,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
