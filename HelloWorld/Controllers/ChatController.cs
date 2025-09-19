using HelloWorld.Data;
using HelloWorld.Hubs;
using HelloWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HelloWorld.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // View chat page with user list
        public IActionResult Index()
        {
            var users = _context.Users.Where(u => u.Id != User.FindFirstValue(ClaimTypes.NameIdentifier)).ToList();
            return View(users);
        }

        // Get messages between current user and another user
        //public async Task<IActionResult> GetMessages(string userId)
        //{
        //    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var messages = await _context.Messages
        //        .Where(m =>
        //            (m.SenderId == currentUserId && m.ReceiverId == userId) ||
        //            (m.SenderId == userId && m.ReceiverId == currentUserId))
        //        .OrderBy(m => m.SentAt)
        //        .ToListAsync();

        //    return Json(messages);
        //}

        // Save message to database
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SaveMessage([FromBody] MessageDTO model)
        {
            if (model == null || string.IsNullOrEmpty(model.ReceiverId) || string.IsNullOrEmpty(model.MessageText))
                return BadRequest("Invalid message data.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = model.ReceiverId,
                Text = model.MessageText,
                SentAt = System.DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] int messageId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
                return NotFound();

            if (message.ReceiverId != currentUserId)
                return Forbid();

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = System.DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Notify sender about the read status in real-time
                await _hubContext.Clients.User(message.SenderId).SendAsync("MessageRead", message.Id);
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId)
                         || (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Text,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync();

            return Json(messages);
        }
    }

    // DTO for SaveMessage
    public class MessageDTO
    {
        public string ReceiverId { get; set; }
        public string MessageText { get; set; }
    }
}