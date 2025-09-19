using HelloWorld.Data;
using HelloWorld.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace HelloWorld.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            // Save message to DB and get messageId
            var msg = new Message { SenderId = senderId, ReceiverId = receiverId, Text = message, SentAt = DateTime.UtcNow, IsRead = false };
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            // Notify receiver with message ID
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message, msg.Id);
        }

        public async Task NotifyTyping(string receiverId)
        {
            await Clients.User(receiverId).SendAsync("UserTyping", Context.UserIdentifier);
        }

        public async Task NotifyMessageRead(int messageId, string senderId)
        {
            await Clients.User(senderId).SendAsync("MessageRead", messageId);
        }

    }
}