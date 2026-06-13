using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Commands.Leave;

// Manager rejects a pending leave application.
// ManagerId comes from JWT claims (the reviewing manager) — never the request body.
// Comment is REQUIRED on rejection so the employee knows WHY (enforced by the validator).
public record RejectLeaveCommand(
    int LeaveApplicationId,
    int ManagerId,
    string Comment
) : IRequest<LeaveApplicationDto>;
