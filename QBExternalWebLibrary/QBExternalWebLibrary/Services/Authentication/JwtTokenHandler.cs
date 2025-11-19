using System.Net.Http.Headers;

namespace QBExternalWebLibrary.Services.Authentication
{
    /// <summary>
    /// HTTP message handler that adds JWT token to Authorization header for all requests.
    /// This ensures the token is sent with EVERY request made through this HttpClient.
    /// </summary>
    public class JwtTokenHandler : DelegatingHandler
    {
        private readonly Func<string?> _getToken;

        public JwtTokenHandler(Func<string?> getToken)
        {
            _getToken = getToken;
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
                Console.WriteLine($"[JwtTokenHandler] Added Authorization header to {request.Method} {request.RequestUri?.PathAndQuery}");
                Console.WriteLine($"[JwtTokenHandler] Token: Bearer {token.Substring(0, Math.Min(30, token.Length))}...");
            }
            else
            {
                Console.WriteLine($"[JwtTokenHandler] No token available for {request.Method} {request.RequestUri?.PathAndQuery}");
            }

            // Continue with the request
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
