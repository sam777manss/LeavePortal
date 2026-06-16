using System;
using System.Collections.Generic;

namespace LeavePortal.Infrastructure.Entities;

public partial class LeaveType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int DefaultDays { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<LeaveApplication> LeaveApplications { get; set; } = new List<LeaveApplication>();

    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}
