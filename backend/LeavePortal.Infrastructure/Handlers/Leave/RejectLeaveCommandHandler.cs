using LeavePortal.Core.Commands.Leave;
using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.DTOs.Notifications;
using LeavePortal.Core.Enums;
using LeavePortal.Core.Interfaces;
using LeavePortal.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Leave;

public class RejectLeaveCommandHandler : IRequestHandler<RejectLeaveCommand, LeaveApplicationDto>
{
    private readonly LeavePortalDbContext _context;
    private readonly IServiceBusPublisher _publisher;

    private const string NotificationQueue = "leave-notifications";

    public RejectLeaveCommandHandler(LeavePortalDbContext context, IServiceBusPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<LeaveApplicationDto> Handle(RejectLeaveCommand request, CancellationToken cancellationToken)
    {
        // Load the application WITH its leave type and applicant.
        // NOTE: no UserId filter here — a manager may act on ANY employee's application.
        var application = await _context.LeaveApplications
            .Include(a => a.LeaveType)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.LeaveApplicationId, cancellationToken);

        if (application is null)
            throw new Exception("Leave application not found.");

        // Business rule: only a Pending application can be rejected.
        if (application.Status != LeaveStatus.Pending.ToString())
            throw new Exception($"Only pending applications can be rejected. Current status: {application.Status}.");

        // Stamp the review audit fields. The comment is required (enforced by the validator)
        // so the employee always learns WHY it was rejected.
        application.Status = LeaveStatus.Rejected.ToString();
        application.ReviewedBy = request.ManagerId;
        application.ReviewComment = request.Comment;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notify (stub) — the employee's request was rejected.
        var message = new LeaveNotificationMessage
        {
            LeaveApplicationId = application.Id,
            EventType = "LeaveRejected",
            EmployeeName = application.User.FullName,
            EmployeeEmail = application.User.Email,
            LeaveType = application.LeaveType.Name,
            StartDate = application.StartDate.ToString("yyyy-MM-dd"),
            EndDate = application.EndDate.ToString("yyyy-MM-dd"),
            TotalDays = application.TotalDays,
            Status = application.Status
        };

        await _publisher.PublishAsync(NotificationQueue, message, cancellationToken);

        return new LeaveApplicationDto
        {
            Id = application.Id,
            LeaveTypeId = application.LeaveTypeId,
            LeaveTypeName = application.LeaveType.Name,
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
}
