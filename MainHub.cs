using Microsoft.AspNetCore.SignalR;

namespace Huna.Signalr
{
    public class MainHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Task.CompletedTask;
            var user = Context.User;
            user?.ToString();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Task.CompletedTask;
        }
    }
}
