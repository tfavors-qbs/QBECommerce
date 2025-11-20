using Microsoft.AspNetCore.Identity;
using QBExternalWebLibrary.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Middleware
{
    /// <summary>
    /// Middleware to detect stale JWT claims and automatically refresh token
    /// </summary>
    public class StaleClaimsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StaleClaimsMiddleware> _logger;

        public StaleClaimsMiddleware(RequestDelegate next, ILogger<StaleClaimsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            UserManager<ApplicationUser> userManager, 
            IConfiguration config,
            IModelService<Client, ClientEditViewModel> clientService)
        {
            // Only check for authenticated users with API requests
            if (context.User.Identity?.IsAuthenticated == true && context.Request.Path.StartsWithSegments("/api"))
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userModifiedAtClaim = context.User.FindFirst("UserModifiedAt")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && !string.IsNullOrEmpty(userModifiedAtClaim))
                {
                    try
                    {
                        // Parse the claim timestamp (ISO 8601 format)
                        if (DateTime.TryParse(userModifiedAtClaim, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime claimTimestamp))
                        {
                            // Get current user from database
                            var user = await userManager.FindByIdAsync(userIdClaim);
                            
                            if (user != null)
                            {
                                // Compare timestamps (with 1 second tolerance for clock skew)
                                if (user.LastModified > claimTimestamp.AddSeconds(1))
                                {
                                    // User has been modified since token was issued
                                    _logger.LogInformation(
                                        "Stale claims detected for user {UserId} ({Email}). Token issued: {TokenTime:O}, User modified: {UserTime:O}. Refreshing token automatically.",
                                        userIdClaim, user.Email, claimTimestamp, user.LastModified);
                                    
                                    // Generate new token with updated claims
                                    var newToken = await GenerateJwtTokenAsync(user, userManager, config, clientService);
                                    
                                    // Set response header to signal client to update token
                                    context.Response.Headers.Append("X-Token-Refresh", newToken);
                                    context.Response.Headers.Append("X-Token-Refreshed", "true");
                                    
                                    _logger.LogInformation("New token generated for user {UserId} ({Email})", userIdClaim, user.Email);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking for stale claims for user {UserId}", userIdClaim);
                        // Don't fail the request, just log the error
                    }
                }
            }

            await _next(context);
        }

        private async Task<string> GenerateJwtTokenAsync(
            ApplicationUser user, 
            UserManager<ApplicationUser> userManager, 
            IConfiguration config,
            IModelService<Client, ClientEditViewModel> clientService)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("UserModifiedAt", user.LastModified.ToString("O"))
            };

            if (!string.IsNullOrEmpty(user.GivenName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, user.GivenName));
                claims.Add(new Claim(ClaimTypes.GivenName, user.GivenName));
            }

            if (!string.IsNullOrEmpty(user.FamilyName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, user.FamilyName));
                claims.Add(new Claim(ClaimTypes.Surname, user.FamilyName));
            }

            if (user.ClientId.HasValue)
            {
                claims.Add(new Claim("ClientId", user.ClientId.Value.ToString()));
                
                // Fetch client name for display purposes
                var client = clientService.GetById(user.ClientId.Value);
                if (client != null && !string.IsNullOrEmpty(client.Name))
                {
                    claims.Add(new Claim("ClientName", client.Name));
                }
            }

            // Add roles
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddMinutes(Convert.ToDouble(config["Jwt:ExpireMinutes"]));

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public static class StaleClaimsMiddlewareExtensions
    {
        public static IApplicationBuilder UseStaleClaimsDetection(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StaleClaimsMiddleware>();
        }
    }
}
