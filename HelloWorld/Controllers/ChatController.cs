using HelloWorld.Data;
using HelloWorld.Hubs;
using HelloWorld.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HelloWorld.Models;


namespace HelloWorld.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChatController> _logger;


        public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext, UserManager<ApplicationUser> userManager, ILogger<ChatController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
            _logger = logger;

        }

        public IActionResult Index()
        {
            var users = _context.Users.Where(u => u.Id != User.FindFirstValue(ClaimTypes.NameIdentifier)).ToList();
            return View(users);
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SaveMessage([FromBody] MessageDTO model)
        {
            if (model == null || string.IsNullOrEmpty(model.ReceiverId) || string.IsNullOrEmpty(model.MessageText) || string.IsNullOrEmpty(model.MessageGuid))
            {
                return BadRequest("Invalid message data.");
            }

            bool exists = await _context.Messages.AnyAsync(m => m.MessageGuid == model.MessageGuid);
            if (exists)
            {
                return Json(new { message = "Duplicate message ignored." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var message = new Message
            {
                MessageGuid = model.MessageGuid,
                SenderId = currentUserId,
                ReceiverId = model.ReceiverId,
                Text = model.MessageText,
                SentAt = System.DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { id = message.Id });
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

                _logger.LogInformation($"Sending 'MessageRead' to user {message.SenderId} for message {message.Id}");

                await _hubContext.Clients.User(message.SenderId).SendAsync("MessageRead", message.Id);
            }
            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(message.SenderId)
                    .SendAsync("MessageRead", message.Id);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageRequest request)
        {
            if (request == null || request.MessageId <= 0)
                return BadRequest("Invalid message ID.");

            var message = await _context.Messages.FindAsync(request.MessageId);

            if (message == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            if (message.SenderId != userId)
            {
                return Forbid();
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.User(userId).SendAsync("MessageDeleted", message.Id);

            return Ok();
        }

        public class DeleteMessageRequest
        {
            public int MessageId { get; set; }
        }


    }

    public class MessageDTO
    {
        public string ReceiverId { get; set; }
        public string MessageText { get; set; }
        public string MessageGuid { get; set; }
    }
}