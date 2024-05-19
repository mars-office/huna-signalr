using Huna.Signalr.Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Huna.Signalr.Consumers
{
    public class SendSignalrMessageRequestConsumer(IHubContext<MainHub> mainHubContext, ILoggerFactory loggerFactory) : IConsumer<SendSignalrMessageRequest>
    {
        private readonly IHubContext<MainHub> _mainHubContext = mainHubContext;
        private readonly ILogger<SendSignalrMessageRequestConsumer> _logger = loggerFactory.CreateLogger<SendSignalrMessageRequestConsumer>();

        public async Task Consume(ConsumeContext<SendSignalrMessageRequest> context)
        {
            _logger.LogInformation("Received message {id}: to->{to} ({receiverType})", context.MessageId, context.Message.To, context.Message.ReceiverType);
            var retryCount = context.Headers.Get<int>("x-retries", 0);
            var newRetry = retryCount + 1;
            try
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
                _logger.LogInformation("Processed message {id}: to->{to} ({receiverType})", context.MessageId, context.Message.To, context.Message.ReceiverType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error for message {id}: to->{to} ({receiverType})", context.MessageId, context.Message.To, context.Message.ReceiverType);
                if (newRetry < 5)
                {
                    await context.Publish(context.Message, x =>
                    {
                        x.Headers.Set("x-retries", newRetry, true);
                    }, context.CancellationToken);
                }
            }
        }
    }
}