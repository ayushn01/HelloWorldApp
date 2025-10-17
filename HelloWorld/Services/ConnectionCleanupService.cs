using HelloWorld.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HelloWorld.Services
{
    public class ConnectionCleanupService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IHubContext<ChatHub> _hubContext;
        private static TimeSpan StaleThreshold = TimeSpan.FromMinutes(1);

        public ConnectionCleanupService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoCleanup, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void DoCleanup(object state)
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in ChatHub.connectionPingTimes)
            {
                var lastPing = kvp.Value;
                if ((now - lastPing) > StaleThreshold)
                {
                    // Connection considered stale, remove it and notify offline if needed
                    ChatHub.connectionPingTimes.TryRemove(kvp.Key, out _);

                    // Remove connection from userConnections dictionary in ChatHub too
                    foreach (var userId in ChatHub.userConnections.Keys)
                    {
                        if (ChatHub.userConnections.TryGetValue(userId, out var connections))
                        {
                            if (connections.Remove(kvp.Key))
                            {
                                if (connections.Count == 0)
                                {
                                    ChatHub.userConnections.TryRemove(userId, out _);
                                    // Notify offline
                                    _hubContext.Clients.All.SendAsync("UserOffline", userId).Wait();
                                }
                            }
                        }
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}