using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Commands.Leave;

// Manager approves a pending leave application.
// ManagerId comes from JWT claims (the reviewing manager) — never the request body.
// Comment is optional on approval (a manager may approve without saying anything).
public record ApproveLeaveCommand(
    int LeaveApplicationId,
    int ManagerId,
    string? Comment
) : IRequest<LeaveApplicationDto>;
