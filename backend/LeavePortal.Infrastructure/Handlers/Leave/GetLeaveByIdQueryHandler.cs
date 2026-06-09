using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.Queries.Leave;
using LeavePortal.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Leave;

public class GetLeaveByIdQueryHandler : IRequestHandler<GetLeaveByIdQuery, LeaveApplicationDto?>
{
    private readonly LeavePortalDbContext _context;

    public GetLeaveByIdQueryHandler(LeavePortalDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveApplicationDto?> Handle(GetLeaveByIdQuery request, CancellationToken cancellationToken)
    {
        // Ownership is enforced in the query itself: id AND userId must both match.
        // If another user's id is requested, this simply returns null → controller returns 404.
        return await _context.LeaveApplications
            .AsNoTracking()
            .Where(a => a.Id == request.LeaveApplicationId && a.UserId == request.UserId)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
