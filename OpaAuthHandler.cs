using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Huna.Signalr
{
    public class OpaAuthHandlerOptions : AuthenticationSchemeOptions
    {

    }

    public class OpaAuthHandler : AuthenticationHandler<OpaAuthHandlerOptions>
    {
        public const string AuthenticationScheme = "OPA";

#pragma warning disable CS0618 // Type or member is obsolete
        public OpaAuthHandler(IOptionsMonitor<OpaAuthHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
#pragma warning restore CS0618 // Type or member is obsolete
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.User == null)
            {
                return await Task.FromResult(AuthenticateResult.NoResult());
            }
            var issuedUtc = Context.Items["issuedUtc"] as DateTimeOffset?;
            var expiresUtc = Context.Items["expiresUtc"] as DateTimeOffset?;

            var ticket = new AuthenticationTicket(Context.User, new AuthenticationProperties { 
                IssuedUtc = issuedUtc,
                ExpiresUtc = expiresUtc,
                AllowRefresh = false,
                IsPersistent = false,
            }, AuthenticationScheme);
            var result = AuthenticateResult.Success(ticket);
            return await Task.FromResult(result);
        }
    }
}
