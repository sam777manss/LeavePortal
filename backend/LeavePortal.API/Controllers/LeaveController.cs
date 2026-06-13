using LeavePortal.Core.Commands.Leave;
using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.Queries.Leave;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeavePortal.API.Controllers;

// [Authorize] at class level — every action requires a valid JWT cookie.
// Both Employees and Managers can apply for / view / cancel their OWN leave,
// so we do NOT restrict by role here.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Reads the authenticated user's id from the JWT claims.
    // This is the trusted source of "who is calling" — never the request body.
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/leave/apply
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveRequest request)
    {
        try
        {
            var command = new ApplyLeaveCommand(
                CurrentUserId,
                request.LeaveTypeId,
                request.StartDate,
                request.EndDate,
                request.Reason);

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/leave/my  — current user's leave history
    [HttpGet("my")]
    public async Task<IActionResult> MyLeaves()
    {
        var result = await _mediator.Send(new GetMyLeavesQuery(CurrentUserId));
        return Ok(result);
    }

    // GET /api/leave/{id}  — a single application the current user owns
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetLeaveByIdQuery(id, CurrentUserId));
        if (result is null)
            return NotFound(new { message = "Leave application not found." });

        return Ok(result);
    }

    // PUT /api/leave/{id}/cancel  — cancel own pending application
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var result = await _mediator.Send(new CancelLeaveCommand(id, CurrentUserId));
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ===================== Manager-only endpoints (Day 4) =====================
    // [Authorize(Roles = "Manager")] is layered ON TOP of the class-level [Authorize].
    // Class level => must be logged in. Method level => must ALSO have the Manager role.
    // A logged-in Employee hitting these gets 403 Forbidden (authenticated but not allowed).

    // GET /api/leave/pending  — all pending applications across all employees
    [HttpGet("pending")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Pending()
    {
        var result = await _mediator.Send(new GetPendingLeavesQuery());
        return Ok(result);
    }

    // PUT /api/leave/{id}/approve  — manager approves a pending application
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveLeaveRequest request)
    {
        try
        {
            // Manager id is CurrentUserId (from JWT claims), NOT from the body.
            var result = await _mediator.Send(
                new ApproveLeaveCommand(id, CurrentUserId, request.Comment));
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT /api/leave/{id}/reject  — manager rejects a pending application (comment required)
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectLeaveRequest request)
    {
        try
        {
            var result = await _mediator.Send(
                new RejectLeaveCommand(id, CurrentUserId, request.Comment));
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
