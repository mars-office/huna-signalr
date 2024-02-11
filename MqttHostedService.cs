using MQTTnet;
using MQTTnet.Client;
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

            var caCert = X509Certificate2.CreateFromPem(
                new ReadOnlySpan<char>(_config["EMQX_CA_CRT"]!.ToCharArray()));
            var caCerts = new X509Certificate2Collection(caCert);

            var splitCerts = _config["EMQX_CLIENT_CRT"]!.Split("-----END CERTIFICATE-----\n");
            var clientCertPem = splitCerts[0] + "-----END CERTIFICATE-----\n";
            var intermediateCertPem = splitCerts[1] + "-----END CERTIFICATE-----\n";

            var clientCert = X509Certificate2.CreateFromPem(
                new ReadOnlySpan<char>(clientCertPem.ToCharArray()),
                new ReadOnlySpan<char>(_config["EMQX_CLIENT_KEY"]!.ToCharArray())
                );


            var intermediateCert = X509Certificate2.CreateFromPem(
                new ReadOnlySpan<char>(intermediateCertPem.ToCharArray())
                );

            var certs = new X509Certificate2Collection(new[]
            {
                
                clientCert,
                intermediateCert,
            });
            

            var tlsOptions = new MqttClientTlsOptionsBuilder()
                .UseTls(true)
                .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12)
                .WithClientCertificates(certs)
                .WithCertificateValidationHandler((certContext) => {
                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    chain.ChainPolicy.VerificationTime = DateTime.Now;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);
                    chain.ChainPolicy.CustomTrustStore.Add(caCert);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                    // convert provided X509Certificate to X509Certificate2
                    var x5092 = new X509Certificate2(certContext.Certificate);

                    return chain.Build(x5092);
                })
                .Build();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("huna-emqx", 8883)
                .WithCleanSession(true)
                .WithCredentials("huna-signalr")
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
