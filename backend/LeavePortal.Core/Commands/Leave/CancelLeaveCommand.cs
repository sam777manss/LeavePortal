using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Commands.Leave;

// Employee cancels their own leave application.
// UserId comes from claims and is used to enforce ownership (you can only cancel YOUR leave).
public record CancelLeaveCommand(
    int LeaveApplicationId,
    int UserId
) : IRequest<LeaveApplicationDto>;
