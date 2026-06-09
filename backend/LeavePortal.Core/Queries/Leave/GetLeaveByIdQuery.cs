using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Queries.Leave;

// Returns a single leave application by id, but only if it belongs to the requesting user.
// Returns null if not found or not owned — the controller turns null into 404.
public record GetLeaveByIdQuery(int LeaveApplicationId, int UserId) : IRequest<LeaveApplicationDto?>;
