using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LeavePortal.API.Hubs;

[Authorize]   // only logged-in users can connect (uses the same JWT cookie)
public class ChatHub : Hub
{
    private readonly LeavePortalDbContext _context;

    public ChatHub(LeavePortalDbContext context)
    {
        _context = context;
    }

    // The connected user's id, from their JWT.
    private int CurrentUserId =>
        int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // The client calls this to send a message to another user.
    public async Task SendMessage(int receiverId, string content)
    {
        // 1. Save the message to the database.
        var message = new Message
        {
            SenderId = CurrentUserId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // 2. Small payload to send to the browsers.
        var payload = new
        {
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.Content,
            message.SentAt
        };

        // 3. Push it live to the RECEIVER and back to the SENDER.
        //    Clients.User(id) targets every open tab of that user.
        await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", payload);
        await Clients.User(CurrentUserId.ToString()).SendAsync("ReceiveMessage", payload);
    }
}