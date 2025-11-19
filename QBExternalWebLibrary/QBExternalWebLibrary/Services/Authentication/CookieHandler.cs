using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Authentication {
    /// <summary>
    /// Handler to ensure credentials are automatically sent over with each request.
    /// Note: This is for Blazor Server - WebAssembly-specific methods are not available.
    /// </summary>
    public class CookieHandler : DelegatingHandler {
        /// <summary>
        /// Main method to override for the handler.
        /// </summary>
        /// <param name="request">The original request.</param>
        /// <param name="cancellationToken">The token to handle cancellations.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            // In Blazor Server, we don't need SetBrowserRequestCredentials (that's WebAssembly-only)
            // The JwtTokenHandler will add the Authorization header
            request.Headers.Add("X-Requested-With", ["XMLHttpRequest"]);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
