namespace Huna.Signalr.Contracts
{
    public class SendSignalrMessageRequest
    {
        public required string ReceiverType { get; set; }
        public required string To { get; set; }
        public required SignalrMessage Message { get; set; }

    }
}