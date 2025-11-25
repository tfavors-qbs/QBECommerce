using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace QBExternalWebLibrary.Services.Authentication
{
    /// <summary>
    /// HTTP message handler that adds JWT token to Authorization header for all requests
    /// and automatically updates the token when the server sends a refreshed one.
    /// </summary>
    public class JwtTokenHandler : DelegatingHandler
    {
        private readonly Func<string?> _getToken;
        private readonly Action<string?> _setToken;
        private readonly ILogger<JwtTokenHandler>? _logger;

        public JwtTokenHandler(Func<string?> getToken, ILogger<JwtTokenHandler>? logger = null)
            : this(getToken, null, logger)
        {
        }

        public JwtTokenHandler(Func<string?> getToken, Action<string?>? setToken, ILogger<JwtTokenHandler>? logger = null)
        {
            _getToken = getToken;
            _setToken = setToken ?? (_ => { }); // No-op if not provided
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
                _logger?.LogDebug("[JwtTokenHandler] Added Authorization header to {Method} {Path} (token length: {Length})", 
                    request.Method, request.RequestUri?.PathAndQuery, token.Length);
            }
            else
            {
                _logger?.LogDebug("[JwtTokenHandler] No token available for {Method} {Path}", 
                    request.Method, request.RequestUri?.PathAndQuery);
            }

            // Continue with the request
            var response = await base.SendAsync(request, cancellationToken);

            // Check if server sent a refreshed token
            if (response.Headers.TryGetValues("X-Token-Refreshed", out var refreshedValues) &&
                refreshedValues.FirstOrDefault() == "true")
            {
                _logger?.LogInformation("[JwtTokenHandler] ? X-Token-Refreshed header detected!");
                
                if (response.Headers.TryGetValues("X-Token-Refresh", out var tokenValues))
                {
                    var newToken = tokenValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        _logger?.LogInformation("[JwtTokenHandler] ?? New token received (length: {Length}), updating local storage", newToken.Length);
                        _setToken(newToken);
                        _logger?.LogInformation("[JwtTokenHandler] ? Token updated successfully");
                    }
                    else
                    {
                        _logger?.LogWarning("[JwtTokenHandler] ?? X-Token-Refresh header present but empty!");
                    }
                }
                else
                {
                    _logger?.LogWarning("[JwtTokenHandler] ?? X-Token-Refreshed is true but X-Token-Refresh header is missing!");
                }
            }

            return response;
        }
    }
}
