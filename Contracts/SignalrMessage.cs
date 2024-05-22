namespace Huna.Signalr.Contracts
{
    public class SignalrMessage
    {
        public required string Type {get;set;}
        public required object Payload { get; set; }
    }
}