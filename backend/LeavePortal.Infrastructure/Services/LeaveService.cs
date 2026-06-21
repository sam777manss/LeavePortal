using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.DTOs.Notifications;
using LeavePortal.Core.Enums;
using LeavePortal.Core.Interfaces;
using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Services;

// All leave operations in one service, moved out of the old MediatR command/query handlers.
// Field validation is done by DataAnnotations on the request DTOs (auto 400);
// the business rules (date sanity, status transitions, ownership) live here as plain checks.
public class LeaveService : ILeaveService
{
    private readonly LeavePortalDbContext _context;
    private readonly IServiceBusPublisher _publisher;

    // Queue name matches the TechDoc config key ServiceBus__QueueName = "leave-notifications".
    private const string NotificationQueue = "leave-notifications";

    public LeaveService(LeavePortalDbContext context, IServiceBusPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    // ===================== Employee operations =====================

    public async Task<LeaveApplicationDto> ApplyAsync(int userId, ApplyLeaveRequest request, CancellationToken cancellationToken = default)
    {
        // Business rules DataAnnotations can't express cleanly (cross-field / "today").
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (request.StartDate < today)
            throw new Exception("Start date cannot be in the past.");
        if (request.EndDate < request.StartDate)
            throw new Exception("End date must be on or after the start date.");

        // Leave type must exist and be active (referential safety beyond the FK).
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId && t.IsActive, cancellationToken);
        if (leaveType is null)
            throw new Exception("Selected leave type does not exist or is inactive.");

        // Applicant must exist and be active (also needed for the notification).
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null)
            throw new Exception("User not found or inactive.");

        // Total days computed on the server — never trusted from the client. Inclusive, hence +1.
        var totalDays = (request.EndDate.DayNumber - request.StartDate.DayNumber) + 1;

        var application = new LeaveApplication
        {
            UserId = userId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            Reason = request.Reason,
            Status = LeaveStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LeaveApplications.Add(application);
        await _context.SaveChangesAsync(cancellationToken);

        // Notify the department's managers — one message per manager (fan-out).
        // The API does NOT send email directly; it hands each notification to the bus and returns fast.
        await PublishToDepartmentManagersAsync(
            user.DepartmentId, application, "LeaveApplied",
            user.FullName, user.Email, leaveType.Name, cancellationToken);

        return MapToDto(application, leaveType.Name);
    }

    public async Task<List<LeaveApplicationDto>> GetMyLeavesAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Read-only → AsNoTracking(). Project straight into the DTO so only needed columns are pulled.
        return await _context.LeaveApplications
            .AsNoTracking()
            .Where(a => a.UserId == userId)
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

    public async Task<LeaveApplicationDto?> GetByIdAsync(int leaveApplicationId, int userId, CancellationToken cancellationToken = default)
    {
        // Ownership enforced in the query: id AND userId must both match. Another user's id → null → 404.
        return await _context.LeaveApplications
            .AsNoTracking()
            .Where(a => a.Id == leaveApplicationId && a.UserId == userId)
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

    public async Task<LeaveApplicationDto> CancelAsync(int leaveApplicationId, int userId, CancellationToken cancellationToken = default)
    {
        // Load WITH leave type and user, enforcing ownership (you can only cancel YOUR leave).
        var application = await _context.LeaveApplications
            .Include(a => a.LeaveType)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == leaveApplicationId && a.UserId == userId, cancellationToken);

        if (application is null)
            throw new Exception("Leave application not found.");

        // Only a Pending application can be cancelled by the employee.
        if (application.Status != LeaveStatus.Pending.ToString())
            throw new Exception($"Only pending applications can be cancelled. Current status: {application.Status}.");

        application.Status = LeaveStatus.Cancelled.ToString();
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notify the department's managers that the request was withdrawn.
        await PublishToDepartmentManagersAsync(
            application.User.DepartmentId, application, "LeaveCancelled",
            application.User.FullName, application.User.Email, application.LeaveType.Name, cancellationToken);

        return MapToDto(application, application.LeaveType.Name);
    }

    // ===================== Manager operations =====================

    public async Task<List<PendingLeaveDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        // Oldest first (FIFO review queue). Projects employee + leave-type names into the DTO in SQL.
        return await _context.LeaveApplications
            .AsNoTracking()
            .Where(a => a.Status == LeaveStatus.Pending.ToString())
            .OrderBy(a => a.CreatedAt)
            .Select(a => new PendingLeaveDto
            {
                Id = a.Id,
                UserId = a.UserId,
                EmployeeName = a.User.FullName,
                EmployeeEmail = a.User.Email,
                LeaveTypeId = a.LeaveTypeId,
                LeaveTypeName = a.LeaveType.Name,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                TotalDays = a.TotalDays,
                Reason = a.Reason,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveApplicationDto> ApproveAsync(int leaveApplicationId, int managerId, string? comment, CancellationToken cancellationToken = default)
    {
        var application = await LoadForReviewAsync(leaveApplicationId, cancellationToken);

        if (application.Status != LeaveStatus.Pending.ToString())
            throw new Exception($"Only pending applications can be approved. Current status: {application.Status}.");
        // --- Day 6: check & deduct the employee's leave balance (for the leave's year) ---
        var year = application.StartDate.Year;

        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.UserId == application.UserId
                                   && b.LeaveTypeId == application.LeaveTypeId
                                   && b.Year == year, cancellationToken);

        if (balance is null)
            throw new Exception($"No leave balance configured for this leave type in {year}.");

        var remaining = balance.TotalDays - balance.UsedDays;
        if (remaining < application.TotalDays)
            throw new Exception($"Insufficient balance: {remaining} day(s) remaining, {application.TotalDays} requested.");

        // Deduct: only UsedDays changes — the DB recomputes RemainingDays.
        balance.UsedDays += application.TotalDays;
        // -----------------------------------------------------------------------------
        // Stamp the review audit fields. ReviewedBy is the manager from the JWT, not the body.
        application.Status = LeaveStatus.Approved.ToString();
        application.ReviewedBy = managerId;
        application.ReviewComment = comment;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await PublishReviewNotificationAsync(application, "LeaveApproved", cancellationToken);

        return MapToDto(application, application.LeaveType.Name);
    }

    public async Task<LeaveApplicationDto> RejectAsync(int leaveApplicationId, int managerId, string comment, CancellationToken cancellationToken = default)
    {
        // [Required] allows whitespace-only; a rejection reason must be real text.
        if (string.IsNullOrWhiteSpace(comment))
            throw new Exception("A comment is required when rejecting a leave application.");

        var application = await LoadForReviewAsync(leaveApplicationId, cancellationToken);

        if (application.Status != LeaveStatus.Pending.ToString())
            throw new Exception($"Only pending applications can be rejected. Current status: {application.Status}.");

        application.Status = LeaveStatus.Rejected.ToString();
        application.ReviewedBy = managerId;
        application.ReviewComment = comment;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await PublishReviewNotificationAsync(application, "LeaveRejected", cancellationToken);

        return MapToDto(application, application.LeaveType.Name);
    }

    // ===================== Helpers =====================

    // Loads an application with its leave type and applicant for a manager review.
    // No UserId filter — a manager may act on ANY employee's application.
    private async Task<LeaveApplication> LoadForReviewAsync(int leaveApplicationId, CancellationToken cancellationToken)
    {
        var application = await _context.LeaveApplications
            .Include(a => a.LeaveType)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == leaveApplicationId, cancellationToken);

        if (application is null)
            throw new Exception("Leave application not found.");

        return application;
    }

    // Apply/cancel notify every active manager in the employee's department (one message each).
    private async Task PublishToDepartmentManagersAsync(
        int departmentId, LeaveApplication application, string eventType,
        string employeeName, string employeeEmail, string leaveTypeName, CancellationToken cancellationToken)
    {
        var managers = await _context.Users
            .Where(u => u.DepartmentId == departmentId
                        && u.Role == UserRole.Manager.ToString()
                        && u.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var manager in managers)
        {
            var message = new LeaveNotificationMessage
            {
                LeaveApplicationId = application.Id,
                EventType = eventType,
                RecipientName = manager.FullName,
                RecipientEmail = manager.Email,
                EmployeeName = employeeName,
                EmployeeEmail = employeeEmail,
                LeaveType = leaveTypeName,
                StartDate = application.StartDate.ToString("yyyy-MM-dd"),
                EndDate = application.EndDate.ToString("yyyy-MM-dd"),
                TotalDays = application.TotalDays,
                Status = application.Status
            };

            await _publisher.PublishAsync(NotificationQueue, message, cancellationToken);
        }
    }

    // Approve/reject notify the employee (recipient = the applicant), carrying the review comment.
    private async Task PublishReviewNotificationAsync(LeaveApplication application, string eventType, CancellationToken cancellationToken)
    {
        var message = new LeaveNotificationMessage
        {
            LeaveApplicationId = application.Id,
            EventType = eventType,
            RecipientName = application.User.FullName,
            RecipientEmail = application.User.Email,
            EmployeeName = application.User.FullName,
            EmployeeEmail = application.User.Email,
            LeaveType = application.LeaveType.Name,
            StartDate = application.StartDate.ToString("yyyy-MM-dd"),
            EndDate = application.EndDate.ToString("yyyy-MM-dd"),
            TotalDays = application.TotalDays,
            Status = application.Status,
            ReviewComment = application.ReviewComment
        };

        await _publisher.PublishAsync(NotificationQueue, message, cancellationToken);
    }

    // Maps a tracked LeaveApplication entity to the response DTO.
    private static LeaveApplicationDto MapToDto(LeaveApplication application, string leaveTypeName) => new()
    {
        Id = application.Id,
        LeaveTypeId = application.LeaveTypeId,
        LeaveTypeName = leaveTypeName,
        StartDate = application.StartDate,
        EndDate = application.EndDate,
        TotalDays = application.TotalDays,
        Reason = application.Reason,
        DocumentUrl = application.DocumentUrl,
        Status = application.Status,
        ReviewComment = application.ReviewComment,
        ReviewedAt = application.ReviewedAt,
        CreatedAt = application.CreatedAt
    };
}
