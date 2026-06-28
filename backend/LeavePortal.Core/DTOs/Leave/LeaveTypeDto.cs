namespace LeavePortal.Core.DTOs.Leave;

// What the frontend dropdown needs: the Id to submit + the Name to show.
public class LeaveTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultDays { get; set; }
}