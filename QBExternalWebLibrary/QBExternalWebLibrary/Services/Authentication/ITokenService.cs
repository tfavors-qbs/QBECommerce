namespace QBExternalWebLibrary.Services.Authentication
{
    /// <summary>
    /// Interface for token storage services
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Get the current JWT token from storage
        /// </summary>
        Task<string?> GetTokenAsync();

        /// <summary>
        /// Set the JWT token in storage
        /// </summary>
        Task SetTokenAsync(string? token);

        /// <summary>
        /// Get token synchronously (for contexts where async is not possible)
        /// Returns cached value, may be stale if storage was updated externally
        /// </summary>
        string? GetTokenSync();

        /// <summary>
        /// Event raised when token changes
        /// </summary>
        event Action<string?>? OnTokenChanged;
    }
}
