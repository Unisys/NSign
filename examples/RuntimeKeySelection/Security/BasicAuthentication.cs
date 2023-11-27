using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace RuntimeKeySelection.Security;

internal static class BasicAuthentication
{
    public const string Scheme = "Basic";

    public static void Default(Options _)
    { }

    internal sealed class Options : AuthenticationSchemeOptions
    { }

    internal sealed class Handler : AuthenticationHandler<Options>
    {
        public Handler(
            IOptionsMonitor<Options> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authValues = Request.Headers.Authorization;
            string? username;

            if (authValues.Count < 1 ||
                !TryExtractUsername(authValues[0], out username))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            Debug.Assert(username != null, "The username must not be null.");

            // Accept any credentials -- do NOT do this in production ever.
            Claim[] claims = new Claim[]
            {
                new Claim(ClaimTypes.Name, username),
            };
            ClaimsIdentity identity = new ClaimsIdentity(claims, BasicAuthentication.Scheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            AuthenticationTicket ticket = new AuthenticationTicket(principal, null, BasicAuthentication.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.Headers.WWWAuthenticate = BasicAuthentication.Scheme + " realm=nsign-examples";

            return Task.CompletedTask;
        }

        private static bool TryExtractUsername(string? authorizationValue, out string? username)
        {
            username = null;
            if (null == authorizationValue)
            {
                return false;
            }

            string[] parts = authorizationValue.Split(' ');
            if (parts.Length < 2 ||
                !String.Equals(BasicAuthentication.Scheme, parts[0], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                byte[] raw = Convert.FromBase64String(parts[1]);
                string rawStr = Encoding.ASCII.GetString(raw);

                username = rawStr.Split(':')[0];
                return true;
            }
            catch (FormatException)
            {
                username = null;
                return false;
            }
        }
    }
}
