using LeavePortal.Core.DTOs.Leave;

namespace LeavePortal.Core.Interfaces;

// All leave operations — apply / view / cancel (employee) and pending / approve / reject (manager).
// The ids that identify "who is calling" (userId / managerId) are passed in by the controller
// from the JWT claims — never taken from the request body.
public interface ILeaveService
{
    Task<LeaveApplicationDto> ApplyAsync(int userId, ApplyLeaveRequest request, string? documentUrl, CancellationToken cancellationToken = default);
    Task<List<LeaveApplicationDto>> GetMyLeavesAsync(int userId, CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto?> GetByIdAsync(int leaveApplicationId, int userId, CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto> CancelAsync(int leaveApplicationId, int userId, CancellationToken cancellationToken = default);

    Task<List<PendingLeaveDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto> ApproveAsync(int leaveApplicationId, int managerId, string? comment, CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto> RejectAsync(int leaveApplicationId, int managerId, string comment, CancellationToken cancellationToken = default);
    Task<string?> GetDocumentUrlAsync(int leaveApplicationId, int userId, bool isManager, CancellationToken cancellationToken = default);
}
