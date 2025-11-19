using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace QBExternalWebLibrary.Services.Authentication
{
    /// <summary>
    /// HTTP message handler that adds JWT token to Authorization header for all requests.
    /// This ensures the token is sent with EVERY request made through this HttpClient.
    /// </summary>
    public class JwtTokenHandler : DelegatingHandler
    {
        private readonly Func<string?> _getToken;
        private readonly ILogger<JwtTokenHandler>? _logger;

        public JwtTokenHandler(Func<string?> getToken, ILogger<JwtTokenHandler>? logger = null)
        {
            _getToken = getToken;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get the current token from the singleton store
            var token = _getToken();
            
            // If we have a token, add it to THIS request's Authorization header
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger?.LogDebug("Added Authorization header to {Method} {Path}", 
                    request.Method, request.RequestUri?.PathAndQuery);
            }

            // Continue with the request
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
