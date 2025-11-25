using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace QBExternalWebLibrary.Services.Authentication {
    /// <summary>
    /// Handles state for cookie-based auth and JWT token-based auth for PunchOut.
    /// Uses ITokenService for proper token management in Blazor Server.
    /// </summary>
    public class CookieAuthenticationStateProvider : AuthenticationStateProvider, IAccountManagement {
        /// <summary>
        /// Map the JavaScript-formatted properties to C#-formatted classes.
        /// </summary>
        private readonly JsonSerializerOptions jsonSerializerOptions =
            new() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

        /// <summary>
        /// Special auth client.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Token service for managing JWT tokens
        /// </summary>
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Logger for diagnostic output
        /// </summary>
        private readonly ILogger<CookieAuthenticationStateProvider> _logger;

        /// <summary>
        /// Authentication state.
        /// </summary>
        private bool _authenticated = false;

        /// <summary>
        /// Default principal for anonymous (not authenticated) users.
        /// </summary>
        private readonly ClaimsPrincipal Unauthenticated =
            new(new ClaimsIdentity());

        /// <summary>
        /// Create a new instance of the auth provider.
        /// </summary>
        /// <param name="httpClientFactory">Factory to retrieve auth client.</param>
        /// <param name="tokenService">Service for managing JWT tokens.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public CookieAuthenticationStateProvider(
            IHttpClientFactory httpClientFactory,
            ITokenService tokenService,
            ILogger<CookieAuthenticationStateProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Auth");
            _tokenService = tokenService;
            _logger = logger;
            
            // Subscribe to token changes
            _tokenService.OnTokenChanged += OnTokenChanged;
            
            _logger.LogInformation("[CookieAuthStateProvider] ?? Provider initialized");
        }

        /// <summary>
        /// Handle token change events
        /// </summary>
        private void OnTokenChanged(string? newToken)
        {
            _logger.LogInformation("[CookieAuthStateProvider] ?? Token change detected via event!");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        /// <summary>
        /// Register a new user.
        /// </summary>
        public async Task<FormResult> RegisterAsync(string email, string password) {
            string[] defaultDetail = ["An unknown error prevented registration from succeeding."];

            try {
                var result = await _httpClient.PostAsJsonAsync(
                    "register", new {
                        email,
                        password
                    });

                if (result.IsSuccessStatusCode) {
                    return new FormResult { Succeeded = true };
                }

                var details = await result.Content.ReadAsStringAsync();
                var problemDetails = JsonDocument.Parse(details);
                var errors = new List<string>();
                var errorList = problemDetails.RootElement.GetProperty("errors");

                foreach (var errorEntry in errorList.EnumerateObject()) {
                    if (errorEntry.Value.ValueKind == JsonValueKind.String) {
                        errors.Add(errorEntry.Value.GetString()!);
                    } else if (errorEntry.Value.ValueKind == JsonValueKind.Array) {
                        errors.AddRange(
                            errorEntry.Value.EnumerateArray().Select(
                                e => e.GetString() ?? string.Empty)
                            .Where(e => !string.IsNullOrEmpty(e)));
                    }
                }

                return new FormResult {
                    Succeeded = false,
                    ErrorList = problemDetails == null ? defaultDetail : [.. errors]
                };
            } catch { }

            return new FormResult {
                Succeeded = false,
                ErrorList = defaultDetail
            };
        }

        /// <summary>
        /// User login with email/password - now returns JWT token.
        /// </summary>
        public async Task<FormResult> LoginAsync(string email, string password) {
            try
            {
                _logger.LogInformation("Starting regular login for user: {Email}", email);
                
                var result = await _httpClient.PostAsJsonAsync(
                    "api/accounts/login", new {
                        email,
                        password
                    });

                if (result.IsSuccessStatusCode) {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, jsonSerializerOptions);
                    
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.accessToken))
                    {
                        // Store the JWT token using TokenService
                        await _tokenService.SetTokenAsync(loginResponse.accessToken);
                        
                        _logger.LogInformation("Token stored successfully for {Email}", email);
                        
                        // Refresh auth state (OnTokenChanged will be called automatically)
                        
                        return new FormResult { Succeeded = true };
                    }
                    else
                    {
                        _logger.LogWarning("Login response or token was null/empty for {Email}", email);
                    }
                }
                else
                {
                    _logger.LogWarning("Regular login failed for {Email}: {StatusCode}", email, result.StatusCode);
                }

                return new FormResult {
                    Succeeded = false,
                    ErrorList = ["Invalid email and/or password."]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regular login exception for {Email}", email);
                return new FormResult {
                    Succeeded = false,
                    ErrorList = [$"Login error: {ex.Message}"]
                };
            }
        }

        /// <summary>
        /// Login for Ariba PunchOut using session ID - returns JWT token.
        /// </summary>
        public async Task<FormResult> LoginAsync(string sessionId)
        {
            try
            {
                _logger.LogInformation("Starting Ariba login with sessionId: {SessionId}", sessionId);
                
                var result = await _httpClient.PostAsJsonAsync("api/accounts/login/ariba", sessionId);

                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, jsonSerializerOptions);
                    
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.accessToken))
                    {
                        // Store the JWT token using TokenService
                        await _tokenService.SetTokenAsync(loginResponse.accessToken);
                        
                        _logger.LogInformation("Ariba token stored successfully for session {SessionId}", sessionId);
                        
                        // Refresh auth state (OnTokenChanged will be called automatically)
                        
                        return new FormResult { Succeeded = true };
                    }
                    else
                    {
                        _logger.LogWarning("Ariba login response or token was null/empty");
                    }
                }
                else
                {
                    _logger.LogWarning("Ariba login failed: {StatusCode}", result.StatusCode);
                }

                return new FormResult
                {
                    Succeeded = false,
                    ErrorList = ["Failed to authenticate with PunchOut session."]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ariba login exception for session {SessionId}", sessionId);
                return new FormResult
                {
                    Succeeded = false,
                    ErrorList = [$"Login error: {ex.Message}"]
                };
            }
        }

        /// <summary>
        /// Get authentication state.
        /// </summary>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
            _authenticated = false;
            var user = Unauthenticated;

            // Check if we have a token first
            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                // No token = not authenticated
                return new AuthenticationState(user);
            }

            try {
                // Decode JWT token to get ALL claims (including UserModifiedAt)
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                
                // Create claims list from JWT token
                var claims = jwtToken.Claims.ToList();
                
                _logger.LogDebug("[CookieAuthStateProvider] Decoded {ClaimCount} claims from JWT token", claims.Count);
                
                // Create authenticated identity with all JWT claims
                var id = new ClaimsIdentity(claims, nameof(CookieAuthenticationStateProvider));
                user = new ClaimsPrincipal(id);
                _authenticated = true;
            } catch (Exception ex) { 
                _logger.LogError(ex, "[CookieAuthStateProvider] Error decoding JWT token");
                // Clear token on any failure
                await _tokenService.SetTokenAsync(null);
            }

            return new AuthenticationState(user);
        }

        public async Task LogoutAsync() {
            await _tokenService.SetTokenAsync(null);

            const string Empty = "{}";
            var emptyContent = new StringContent(Empty, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("api/accounts/logout", emptyContent);
            
            // OnTokenChanged will be called automatically
        }

        public async Task<bool> CheckAuthenticatedAsync() {
            await GetAuthenticationStateAsync();
            return _authenticated;
        }

        public class RoleClaim {
            public string? Issuer { get; set; }
            public string? OriginalIssuer { get; set; }
            public string? Type { get; set; }
            public string? Value { get; set; }
            public string? ValueType { get; set; }
        }
    }
}
