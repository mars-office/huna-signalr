using Huna.Signalr.Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Huna.Signalr.Consumers
{
    public class SendSignalrMessageRequestConsumer(IHubContext<MainHub> mainHubContext) : IConsumer<SendSignalrMessageRequest>
    {
        private readonly IHubContext<MainHub> _mainHubContext = mainHubContext;

        public async Task Consume(ConsumeContext<SendSignalrMessageRequest> context)
        {
            IClientProxy? clientProxy = null;
            if (context.Message.ReceiverType == "all")
            {
                clientProxy = _mainHubContext.Clients.All;
            }
            else if (context.Message.ReceiverType == "user")
            {
                clientProxy = _mainHubContext.Clients.User(context.Message.To!);
            }
            else if (context.Message.ReceiverType == "group")
            {
                clientProxy = _mainHubContext.Clients.Group(context.Message.To!);
            }
            if (clientProxy != null)
            {
                await clientProxy.SendAsync("receiveData", context.Message.Payload);
            }
        }
    }
}