using Microsoft.Extensions.Logging;
using QBExternalWebLibrary.Services.Authentication;
using System.Collections.Concurrent;

namespace ShopQualityboltWebBlazor.Services
{
    /// <summary>
    /// Service for managing JWT tokens in a Blazor Server environment.
    /// Uses a static concurrent dictionary keyed by circuit ID for reliable token access across scopes.
    /// This ensures proper isolation between users and works reliably with HTTP requests.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly CircuitIdAccessor _circuitIdAccessor;
        
        // Static storage shared across all instances, but keyed by circuit ID for isolation
        private static readonly ConcurrentDictionary<string, TokenData> _tokens = new();

        public TokenService(ILogger<TokenService> logger, CircuitIdAccessor circuitIdAccessor)
        {
            _logger = logger;
            _circuitIdAccessor = circuitIdAccessor;
        }

        private string GetCircuitId()
        {
            var circuitId = _circuitIdAccessor.CircuitId;
            if (string.IsNullOrEmpty(circuitId))
            {
                // Fallback if circuit ID is not available
                circuitId = "default";
                _logger.LogWarning("[TokenService] ?? Circuit ID not available, using default");
            }
            return circuitId;
        }

        /// <summary>
        /// Get the current JWT token
        /// </summary>
        public Task<string?> GetTokenAsync()
        {
            var circuitId = GetCircuitId();
            var hasToken = _tokens.TryGetValue(circuitId, out var tokenData);
            _logger.LogInformation("[TokenService] Getting token for circuit {CircuitId}: {HasToken}, Token length: {Length}", 
                circuitId, hasToken, tokenData?.Token?.Length ?? 0);
            return Task.FromResult(tokenData?.Token);
        }

        /// <summary>
        /// Set the JWT token
        /// </summary>
        public Task SetTokenAsync(string? token)
        {
            var circuitId = GetCircuitId();
            
            if (string.IsNullOrEmpty(token))
            {
                _tokens.TryRemove(circuitId, out _);
                _logger.LogInformation("[TokenService] ? Token cleared for circuit {CircuitId}", circuitId);
            }
            else
            {
                _tokens[circuitId] = new TokenData { Token = token, LastUpdated = DateTime.UtcNow };
                _logger.LogInformation("[TokenService] ? Token saved for circuit {CircuitId} (length: {Length})", circuitId, token.Length);
            }

            // Raise event to notify listeners of token change
            OnTokenChanged?.Invoke(token);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get token synchronously
        /// </summary>
        public string? GetTokenSync()
        {
            var circuitId = GetCircuitId();
            _tokens.TryGetValue(circuitId, out var tokenData);
            return tokenData?.Token;
        }

        /// <summary>
        /// Event raised when token changes
        /// </summary>
        public event Action<string?>? OnTokenChanged;

        private class TokenData
        {
            public string? Token { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    }
}
