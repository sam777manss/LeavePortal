using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Queries.Leave;

// Query (read) — returns ALL leave applications with status 'Pending',
// across every employee, for a manager to review.
// Unlike GetMyLeavesQuery there is no UserId filter: a manager sees everyone's
// pending requests. Authorization (Manager-only) is enforced at the controller.
public record GetPendingLeavesQuery() : IRequest<List<PendingLeaveDto>>;
