using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Commands.Leave;

// The command that actually flows through MediatR.
// UserId is set by the controller from the authenticated JWT claims (NOT from the body).
public record ApplyLeaveCommand(
    int UserId,
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason
) : IRequest<LeaveApplicationDto>;
