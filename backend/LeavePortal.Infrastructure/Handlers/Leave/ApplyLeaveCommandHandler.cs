using LeavePortal.Core.Commands.Leave;
using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.DTOs.Notifications;
using LeavePortal.Core.Enums;
using LeavePortal.Core.Interfaces;
using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LeavePortal.Infrastructure.Handlers.Leave;

public class ApplyLeaveCommandHandler : IRequestHandler<ApplyLeaveCommand, LeaveApplicationDto>
{
    private readonly LeavePortalDbContext _context;
    private readonly IServiceBusPublisher _publisher;

    // Queue name matches the TechDoc config key ServiceBus__QueueName = "leave-notifications"
    private const string NotificationQueue = "leave-notifications";

    public ApplyLeaveCommandHandler(LeavePortalDbContext context, IServiceBusPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<LeaveApplicationDto> Handle(ApplyLeaveCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate the leave type exists and is active (referential safety beyond the FK)
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId && t.IsActive, cancellationToken);

        if (leaveType is null)
            throw new Exception("Selected leave type does not exist or is inactive.");

        // 2. Load the applicant (needed for the notification, and confirms the user exists/active)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            throw new Exception("User not found or inactive.");

        // 3. Total days is computed on the server — never trusted from the client.
        //    Inclusive of both start and end date, hence +1.
        var totalDays = (request.EndDate.DayNumber - request.StartDate.DayNumber) + 1;

        // 4. Create the application in Pending state.
        var application = new LeaveApplication
        {
            UserId = request.UserId,
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

        // 5. Publish a notification message (stub logs it today; Azure Function consumes it on Day 5).
        //    The API does NOT send email directly — it hands off to the bus and returns fast.
        var message = new LeaveNotificationMessage
        {
            LeaveApplicationId = application.Id,
            EventType = "LeaveApplied",
            EmployeeName = user.FullName,
            EmployeeEmail = user.Email,
            LeaveType = leaveType.Name,
            StartDate = application.StartDate.ToString("yyyy-MM-dd"),
            EndDate = application.EndDate.ToString("yyyy-MM-dd"),
            TotalDays = application.TotalDays,
            Status = application.Status
        };

        await _publisher.PublishAsync(NotificationQueue, message, cancellationToken);

        // 6. Return a clean DTO.
        return new LeaveApplicationDto
        {
            Id = application.Id,
            LeaveTypeId = leaveType.Id,
            LeaveTypeName = leaveType.Name,
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
