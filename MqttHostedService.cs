﻿using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using System.Security.Cryptography.X509Certificates;

namespace Huna.Signalr
{
    public class MqttHostedService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttFactory _mqttFactory = new();
        private readonly ILogger<MqttHostedService> _logger;
        private readonly MqttClientOptions _options;
        private readonly IConfiguration _config;
        private readonly IHubContext<MainHub> _mainHubContext;

        public MqttHostedService(ILogger<MqttHostedService> logger, IConfiguration config, IHubContext<MainHub> mainHubContext)
        {
            _logger = logger;
            _config = config;
            _mainHubContext = mainHubContext;

            var caCerts = new X509Certificate2Collection();
            caCerts.ImportFromPem(_config["EMQX_CA_CRT"]!);


            var clientCerts = new X509Certificate2Collection(new[] {
                new X509Certificate2(X509Certificate2.CreateFromPem(_config["EMQX_CLIENT_CRT"]!, _config["EMQX_CLIENT_KEY"]!).Export(X509ContentType.Pfx)),
            });


            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("huna-emqx", 8883)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .WithClientId(Environment.MachineName)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithTlsOptions(new MqttClientTlsOptionsBuilder()
                    .UseTls(true)
                    .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12)
                    .WithClientCertificates(clientCerts)
                    .WithIgnoreCertificateRevocationErrors(true)
                    .WithTrustChain(caCerts)
                    .Build())
                .Build();

            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClient.DisconnectedAsync += async e =>
            {
                logger.LogError("MQTTClient disconnected. Waiting 5 seconds...");
                await Task.Delay(5000);
                try
                {
                    _logger.LogError("MQTTClient reconnecting...");
                    await _mqttClient.ConnectAsync(_options, CancellationToken.None);
                    _logger.LogError("MQTTClient reconnected.");
                }
                catch (Exception err)
                {
                    _logger.LogError(err, "MQTT reconnect failed.");
                }
            };

            _mqttClient.ConnectedAsync += async e =>
            {
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("mainHub/#", MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                    .Build();
                await _mqttClient.SubscribeAsync(subscribeOptions);
            };

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                await Task.CompletedTask;
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (OperatingSystem.IsLinux())
            {
                await File.WriteAllTextAsync("/etc/ssl/certs/mqtt.crt", _config["EMQX_CLIENT_CRT"]!, cancellationToken);
            }
            try
            {
                await _mqttClient.ConnectAsync(_options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT ERROR");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        }
    }
}
