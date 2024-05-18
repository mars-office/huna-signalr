using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Huna.Notifications.Contracts;
using MassTransit;

namespace Huna.Signalr
{
    public class TestServuce : IHostedService
    {
        private readonly IBus _bus;

        public TestServuce(IBus bus)
        {
            _bus = bus;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
       
                await _bus.Publish(new NotificationRequest
                {
                    Title = "xxxxxxx"
                }, cancellationToken: cancellationToken);

            Console.WriteLine("Doneeee");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}