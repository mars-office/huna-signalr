using MQTTnet;
using MQTTnet.Client;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Huna.Signalr
{
    public class MqttHostedService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttFactory _mqttFactory = new MqttFactory();
        private readonly ILogger<MqttHostedService> _logger;
        private readonly MqttClientOptions _options;
        private readonly IConfiguration _config;

        public MqttHostedService(ILogger<MqttHostedService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            var tempClientCerts = new X509Certificate2Collection();
            tempClientCerts.ImportFromPem(_config["EMQX_CLIENT_CRT"]!);

            var clientCerts = new X509Certificate2Collection();
            var rsaKey = RSA.Create();
            rsaKey.ImportFromPem(_config["EMQX_CLIENT_KEY"]!);
            clientCerts.Add(tempClientCerts[0].CopyWithPrivateKey(rsaKey));
            clientCerts[0].Verify();

            var caCerts = new X509Certificate2Collection();
            caCerts.ImportFromPem(_config["EMQX_CA_CRT"]!);
            caCerts.Add(tempClientCerts[1]);
            



            var tlsOptions = new MqttClientTlsOptionsBuilder()
                .UseTls(true)
                .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12)
                .WithClientCertificates(clientCerts)
                .WithTrustChain(caCerts)
                .WithIgnoreCertificateRevocationErrors(true)
                .Build();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("huna-emqx", 8883)
                .WithCleanSession(true)
                .WithClientId(Environment.MachineName)
                .WithTlsOptions(tlsOptions)
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
                    .WithTopicFilter("mainHub")
                    .Build();
                await _mqttClient.SubscribeAsync(subscribeOptions);
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _mqttClient.ConnectAsync(_options, cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        }
    }
}
