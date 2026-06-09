using LeavePortal.Core.DTOs.Leave;
using MediatR;

namespace LeavePortal.Core.Queries.Leave;

// Query (read) — returns all leave applications belonging to the current user.
// This is the "Q" in CQRS: queries read data and never change it.
public record GetMyLeavesQuery(int UserId) : IRequest<List<LeaveApplicationDto>>;
