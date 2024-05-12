namespace Huna.Signalr.Contracts
{
    public class SendSignalrMessageRequest
    {
        public string? ReceiverType { get; set; }
        public string? To { get; set; }
        public object? Payload { get; set; }

    }
}