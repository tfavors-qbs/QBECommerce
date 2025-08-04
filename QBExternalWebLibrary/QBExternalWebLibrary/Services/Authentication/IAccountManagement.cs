
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;

namespace QBExternalWebLibrary.Services.Authentication {
    /// <summary>
    /// Account management services.
    /// </summary>
    public interface IAccountManagement {
        /// <summary>
        /// Login service.
        /// </summary>
        /// <param name="email">User's email.</param>
        /// <param name="password">User's password.</param>
        /// <returns>The result of the request serialized to <see cref="FormResult"/>.</returns>
        public Task<FormResult> LoginAsync(string email, string password);

        public Task<FormResult> LoginAsync(string sessionId);

        /// <summary>
        /// Log out the logged in user.
        /// </summary>
        /// <returns>The asynchronous task.</returns>
        public Task LogoutAsync();

        /// <summary>
        /// Registration service.
        /// </summary>
        /// <param name="email">User's email.</param>
        /// <param name="password">User's password.</param>
        /// <returns>The result of the request serialized to <see cref="FormResult"/>.</returns>
        public Task<FormResult> RegisterAsync(string email, string password);

        public Task<bool> CheckAuthenticatedAsync();
    }
}
