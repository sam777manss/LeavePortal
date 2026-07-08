using LeavePortal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeavePortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly LeavePortalDbContext _context;

    public ChatController(LeavePortalDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/chat/users  — everyone you can chat with
    [HttpGet("users")]
    public async Task<IActionResult> GetChatUsers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive && u.Id != CurrentUserId)
            .Select(u => new { u.Id, u.FullName, u.Role })
            .ToListAsync();

        return Ok(users);
    }

    // GET /api/chat/history/{otherUserId}  — all messages between me and one other user
    [HttpGet("history/{otherUserId:int}")]
    public async Task<IActionResult> GetHistory(int otherUserId)
    {
        var me = CurrentUserId;

        var messages = await _context.Messages
            .Where(m =>
                (m.SenderId == me && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == me))
            .OrderBy(m => m.SentAt)
            .Select(m => new { m.Id, m.SenderId, m.ReceiverId, m.Content, m.SentAt })
            .ToListAsync();

        return Ok(messages);
    }
}