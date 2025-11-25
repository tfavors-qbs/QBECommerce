using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace QBExternalWebLibrary.Services.Authentication
{
    /// <summary>
    /// HTTP message handler that adds JWT token to Authorization header for all requests
    /// and automatically updates the token when the server sends a refreshed one.
    /// Uses ITokenService for proper token management in Blazor Server.
    /// </summary>
    public class JwtTokenHandler : DelegatingHandler
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<JwtTokenHandler>? _logger;

        public JwtTokenHandler(ITokenService tokenService, ILogger<JwtTokenHandler>? logger = null)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get token - simple and reliable
            var token = await _tokenService.GetTokenAsync();
            
            // Add to Authorization header if available
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger?.LogDebug("[JwtTokenHandler] ? Authorization header added (token length: {Length})", token.Length);
            }
            else
            {
                _logger?.LogDebug("[JwtTokenHandler] ?? No token available for {Method} {Path}", 
                    request.Method, request.RequestUri?.PathAndQuery);
            }

            // Send request
            var response = await base.SendAsync(request, cancellationToken);

            // Check for token refresh from server
            if (response.Headers.TryGetValues("X-Token-Refreshed", out var refreshedValues) &&
                refreshedValues.FirstOrDefault() == "true")
            {
                _logger?.LogInformation("[JwtTokenHandler] ?? Server sent refreshed token");
                
                if (response.Headers.TryGetValues("X-Token-Refresh", out var tokenValues))
                {
                    var newToken = tokenValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        _logger?.LogInformation("[JwtTokenHandler] ?? Saving refreshed token (length: {Length})", newToken.Length);
                        await _tokenService.SetTokenAsync(newToken);
                    }
                }
            }

            return response;
        }
    }
}
