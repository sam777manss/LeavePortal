using System;
using System.Collections.Generic;

namespace LeavePortal.Infrastructure.Entities;

public partial class LeaveApplication
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int TotalDays { get; set; }

    public string Reason { get; set; } = null!;

    public string? DocumentUrl { get; set; }

    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public string? ReviewComment { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual LeaveType LeaveType { get; set; } = null!;

    public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
