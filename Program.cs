namespace Huna.Signalr;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "env.json", true, true);


        var app = builder.Build();

        app.MapGet("/api/signalr/health", (HttpContext context) => {
            return "OK";
        }).WithName("Healthcheck");


        app.Run("http://+:3000");
    }
}
