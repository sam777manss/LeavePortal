using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.Queries.Leave;
using LeavePortal.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Leave;

public class GetMyLeavesQueryHandler : IRequestHandler<GetMyLeavesQuery, List<LeaveApplicationDto>>
{
    private readonly LeavePortalDbContext _context;

    public GetMyLeavesQueryHandler(LeavePortalDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveApplicationDto>> Handle(GetMyLeavesQuery request, CancellationToken cancellationToken)
    {
        // Read-only query. AsNoTracking() because we never modify these entities —
        // it skips EF change-tracking overhead and is faster for pure reads.
        // We project straight into the DTO in the SQL query (Select) so only the
        // columns we need are pulled from the database.
        return await _context.LeaveApplications
            .AsNoTracking()
            .Where(a => a.UserId == request.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new LeaveApplicationDto
            {
                Id = a.Id,
                LeaveTypeId = a.LeaveTypeId,
                LeaveTypeName = a.LeaveType.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                TotalDays = a.TotalDays,
                Reason = a.Reason,
                DocumentUrl = a.DocumentUrl,
                Status = a.Status,
                ReviewComment = a.ReviewComment,
                ReviewedAt = a.ReviewedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
