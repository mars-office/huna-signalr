using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Huna.Signalr;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var tracingOtlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var otel = builder.Services.AddOpenTelemetry();
        otel.ConfigureResource(resource => resource
            .AddService(serviceName: "huna-signalr"));
        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            if (tracingOtlpEndpoint != null && !builder.Environment.IsDevelopment())
            {
                tracing.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
                });
            }
            else
            {
                tracing.AddConsoleExporter();
            }
        });

        builder.Configuration.AddJsonFile(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "env.json", true, true);

        builder.Services.AddSignalR(ho =>
        {
            ho.EnableDetailedErrors = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
        }).AddStackExchangeRedis("huna-redis:6379", rco =>
        {
            rco.Configuration.ChannelPrefix = RedisChannel.Literal("HunaSignalr");
            rco.Configuration.AllowAdmin = true;
            rco.Configuration.ClientName = Environment.MachineName;
            rco.Configuration.Password = builder.Configuration["REDIS_PASSWORD"];
            rco.Configuration.IncludeDetailInExceptions = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
        });

        builder.Services.AddHttpClient();
        builder.Services.AddAuthentication(OpaAuthHandler.AuthenticationScheme)
            .AddScheme<OpaAuthHandlerOptions, OpaAuthHandler>(OpaAuthHandler.AuthenticationScheme, o =>
            {

            });


        var app = builder.Build();

        app.MapGet("/api/signalr/health", (HttpContext context) =>
        {
            return "OK";
        }).WithName("Healthcheck");

        app.UseMiddleware<OpaMiddleware>();
        app.UseAuthentication();

        app.MapHub<MainHub>("/api/signalr/mainHub", hcdo =>
        {
            hcdo.CloseOnAuthenticationExpiration = true;
        });

        app.Run("http://+:3005");
    }

}
