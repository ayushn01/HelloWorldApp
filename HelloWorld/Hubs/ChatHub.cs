using HelloWorld.Data;
using HelloWorld.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace HelloWorld.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        // Track user IDs to multiple connection IDs
        public static ConcurrentDictionary<string, HashSet<string>> userConnections = new ConcurrentDictionary<string, HashSet<string>>();
        public static ConcurrentDictionary<string, DateTime> connectionPingTimes = new ConcurrentDictionary<string, DateTime>();

        public Task Heartbeat()
        {
            connectionPingTimes[Context.ConnectionId] = DateTime.UtcNow;
            return Task.CompletedTask;
        }


        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"Connected: userId={userId}, connectionId={Context.ConnectionId}");
            if (!string.IsNullOrEmpty(userId))
            {
                userConnections.AddOrUpdate(userId,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, existingConnections) =>
                    {
                        existingConnections.Add(Context.ConnectionId);
                        return existingConnections;
                    });
                var onlineUsers = userConnections.Keys;
                await Clients.Caller.SendAsync("InitialOnlineUsers", onlineUsers);
                await Clients.All.SendAsync("UserOnline", userId);
                Console.WriteLine($"UserOnline event sent for userId: {userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                if (userConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        userConnections.TryRemove(userId, out _);
                        await Clients.All.SendAsync("UserOffline", userId);
                        Console.WriteLine($"UserOffline event sent for userId: {userId}");
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string currentUserId, string selectedUserId, string text, string? messageGuid)
        {
            if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(selectedUserId) || string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("SendMessage received invalid parameters.");
                return;
            }

            if (string.IsNullOrEmpty(messageGuid))
                messageGuid = Guid.NewGuid().ToString();

            if (await _context.Messages.AnyAsync(m => m.MessageGuid == messageGuid))
            {
                Console.WriteLine($"Duplicate message detected: {messageGuid}");
                return;
            }

            var msg = new Message
            {
                SenderId = currentUserId,
                ReceiverId = selectedUserId,
                Text = text,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                MessageGuid = messageGuid
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            // Send ReceiveMessage to all connections of receiver
            if (userConnections.TryGetValue(selectedUserId, out var receiverConnectionIds))
            {
                foreach (var connectionId in receiverConnectionIds)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", currentUserId, text, msg.Id);
                }
            }

            if (userConnections.TryGetValue(currentUserId, out var senderConnectionIds))
            {
                foreach (var connectionId in senderConnectionIds)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", currentUserId, text, msg.Id);
                }
            }
        }

        public async Task NotifyTyping(string receiverId)
        {
            if (userConnections.TryGetValue(receiverId, out var connectionIds))
            {
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).SendAsync("UserTyping", Context.UserIdentifier);
                }
            }
        }

        public async Task NotifyMessageRead(int messageId, string senderId)
        {
            if (userConnections.TryGetValue(senderId, out var connectionIds))
            {
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).SendAsync("MessageRead", messageId);
                }
            }
        }

        public async Task NotifyMessageDeleted(int messageId, string userId)
        {
            if (userConnections.TryGetValue(userId, out var connectionIds))
            {
                foreach (var connectionId in connectionIds)
                {
                    await Clients.Client(connectionId).SendAsync("MessageDeleted", messageId);
                }
            }
        }

        public static HashSet<string> GetUserConnections(string userId)
        {
            if (userConnections.TryGetValue(userId, out var connections))
                return connections;
            return new HashSet<string>();
        }

    }
}
