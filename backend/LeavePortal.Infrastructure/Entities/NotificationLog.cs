using System;
using System.Collections.Generic;

namespace LeavePortal.Infrastructure.Entities;

public partial class NotificationLog
{
    public int Id { get; set; }

    public int LeaveApplicationId { get; set; }

    public string RecipientEmail { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public DateTime SentAt { get; set; }

    public virtual LeaveApplication LeaveApplication { get; set; } = null!;
}
