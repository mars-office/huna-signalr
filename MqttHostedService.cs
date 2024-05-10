using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

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

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("huna-emqx", 1883)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .WithClientId(Environment.MachineName)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithTlsOptions(new MqttClientTlsOptionsBuilder()
                    .UseTls(false)
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
                await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("$share/main/mainHub/group/+", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                    .Build());
                await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("$share/main/mainHub/user/+", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                    .Build());
                await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("$share/main/mainHub/all", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                    .Build());
            };

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var splitTopic = e.ApplicationMessage.Topic.Split("/");
                IClientProxy? clientProxy = null;
                if (splitTopic[1] == "all")
                {
                    clientProxy = _mainHubContext.Clients.All;
                } else if (splitTopic[1] == "user")
                {
                    clientProxy = _mainHubContext.Clients.User(splitTopic[2]);
                } else if (splitTopic[1] == "group")
                {
                    clientProxy = _mainHubContext.Clients.Group(splitTopic[2]);
                }
                if (clientProxy != null)
                {
                    var payloadString = e.ApplicationMessage.ConvertPayloadToString();
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(payloadString));
                    var payload = await JsonSerializer.DeserializeAsync<object>(ms);
                    await clientProxy.SendAsync("receiveData", payload);
                }
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
