using LeavePortal.Core.DTOs.Leave;
using LeavePortal.Core.Interfaces;
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
    private readonly ILeaveService _leaveService;
    private readonly IBlobStorageService _blobStorage;

    public LeaveController(ILeaveService leaveService, IBlobStorageService blobStorageService)
    {
        _leaveService = leaveService;
        _blobStorage = blobStorageService;
    }

    // Reads the authenticated user's id from the JWT claims.
    // This is the trusted source of "who is calling" — never the request body.
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/leave/apply  — now accepts multipart/form-data (fields + optional file)
    [HttpPost("apply")] 
    public async Task<IActionResult> Apply([FromForm] ApplyLeaveRequest request, IFormFile? document)
    {
        try
        {
            string? documentUrl = null;
            if (document is not null && document.Length > 0)
            {
                await using var stream = document.OpenReadStream();
                documentUrl = await _blobStorage.UploadAsync(stream, document.FileName, document.ContentType);
            }

            var result = await _leaveService.ApplyAsync(CurrentUserId, request, documentUrl);
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
        var result = await _leaveService.GetMyLeavesAsync(CurrentUserId);
        return Ok(result);
    }

    // GET /api/leave/{id}  — a single application the current user owns
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _leaveService.GetByIdAsync(id, CurrentUserId);
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
            var result = await _leaveService.CancelAsync(id, CurrentUserId);
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
        var result = await _leaveService.GetPendingAsync();
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
            var result = await _leaveService.ApproveAsync(id, CurrentUserId, request.Comment);
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
            var result = await _leaveService.RejectAsync(id, CurrentUserId, request.Comment);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/leave/{id}/document  — securely download the attached file (owner or any manager)
    [HttpGet("{id:int}/document")]
    public async Task<IActionResult> GetDocument(int id)
    {
        try
        {
            var isManager = User.IsInRole("Manager");
            var documentUrl = await _leaveService.GetDocumentUrlAsync(id, CurrentUserId, isManager);

            if (string.IsNullOrEmpty(documentUrl))
                return NotFound(new { message = "No document attached to this application." });

            var (content, contentType) = await _blobStorage.DownloadAsync(documentUrl);
            return File(content, contentType);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/leave/types  — active leave types for the apply form dropdown.
    // Any logged-in user can read these (class-level [Authorize] still applies).
    [HttpGet("types")]
    public async Task<IActionResult> LeaveTypes()
    {
        var result = await _leaveService.GetLeaveTypesAsync();
        return Ok(result);
    }
}
