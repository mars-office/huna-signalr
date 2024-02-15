using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Huna.Signalr
{
    class OpaResponseUser
    {
        public string? Sub { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Aud { get; set; }
        public long Exp { get; set; }
        public long Iat { get; set; }
    }

    class OpaResponse
    {
        public OpaResponseResult? Result { get; set; }
        [JsonPropertyName("decision_id")]
        public string? DecisionId { get; set; }
    }

    class OpaResponseResult
    {
        [JsonPropertyName("is_admin")]
        public bool IsAdmin { get; set; }
        public bool Allow { get; set; }
        public OpaResponseUser? User { get; set; }

    }

    class OpaInputPayload
    {
        public string? Method { get; set; }
        public IDictionary<string, string>? Headers { get; set; }
        public string? Url { get; set; }
        public string? Type { get; set; }
        public string? RemoteAddress { get; set; }
        public string? Service { get; set; }
    }

    class OpaInput
    {
        public OpaInputPayload? Input { get; set; }
    }

    public class OpaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;

        public OpaMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory)
        {
            _next = next;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/signalr/health", StringComparison.InvariantCultureIgnoreCase))
            {
                await _next(context);
                return;
            }

            var headersDict = context.Request.Headers.ToDictionary(x => x.Key.ToLower(), x => x.Value.ToString());

            if (context.Request.Path.StartsWithSegments("/api/signalr/mainhub", StringComparison.InvariantCultureIgnoreCase) && context.Request.Query.ContainsKey("access_token"))
            {
                headersDict["authorization"] = "Bearer " + context.Request.Query["access_token"];
            }

            var opaHttpResponse = await _httpClient.PostAsJsonAsync("http://localhost:8181/v1/data/com/huna/authz", new OpaInput
            {
                Input = new OpaInputPayload
                {
                    Url = context.Request.Path.ToString(),
                    Method = context.Request.Method,
                    Headers = headersDict,
                    Type = "oauth",
                    Service = "huna-signalr",
                    RemoteAddress = context.Connection.RemoteIpAddress?.ToString()
                }
            }, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            if (!opaHttpResponse.IsSuccessStatusCode)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var response = await opaHttpResponse.Content.ReadFromJsonAsync<OpaResponse>(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            if (response?.Result == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            if (!response.Result.Allow)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            if (response.Result.User != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, response.Result.User.Name!),
                    new(ClaimTypes.Email, response.Result.User.Email!),
                    new(ClaimTypes.NameIdentifier, response.Result.User.Email!),
                    new("sub", response.Result.User.Sub!),
                    new("isAdmin", response.Result.IsAdmin.ToString()),
                    new(ClaimTypes.Role, response.Result.IsAdmin ? "admin" : "user")
                };
                var identity = new ClaimsIdentity(claims, "OPA");
                var principal = new ClaimsPrincipal(identity);

                var expiresUtc = DateTimeOffset.FromUnixTimeSeconds(response.Result.User.Exp);
                var issuedUtc = DateTimeOffset.FromUnixTimeSeconds(response.Result.User.Iat);
                context.Items.Add("expiresUtc", expiresUtc);
                context.Items.Add("issuedUtc", issuedUtc);
                context.User = principal;
            }

            await _next(context);
        }
    }
}
