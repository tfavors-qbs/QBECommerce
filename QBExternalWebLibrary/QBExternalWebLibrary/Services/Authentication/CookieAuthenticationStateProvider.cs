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
        /// Token getter function - injected from DI
        /// </summary>
        private readonly Func<string?> _getToken;
        
        /// <summary>
        /// Token setter action - injected from DI
        /// </summary>
        private readonly Action<string?> _setToken;

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
        /// Track last token to detect automatic refreshes
        /// </summary>
        private string? _lastToken;

        /// <summary>
        /// Create a new instance of the auth provider.
        /// </summary>
        /// <param name="httpClientFactory">Factory to retrieve auth client.</param>
        /// <param name="getToken">Function to get current JWT token.</param>
        /// <param name="setToken">Action to set JWT token.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public CookieAuthenticationStateProvider(
            IHttpClientFactory httpClientFactory,
            Func<string?> getToken,
            Action<string?> setToken,
            ILogger<CookieAuthenticationStateProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Auth");
            _getToken = getToken;
            _setToken = setToken;
            _logger = logger;
            _lastToken = getToken();
            
            // Start a background task to monitor token changes
            _ = MonitorTokenChangesAsync();
        }

        /// <summary>
        /// Monitor for token changes and refresh auth state when token is updated
        /// </summary>
        private async Task MonitorTokenChangesAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(1000); // Check every second
                    
                    var currentToken = _getToken();
                    if (currentToken != _lastToken && !string.IsNullOrEmpty(currentToken))
                    {
                        _logger.LogInformation("Token change detected, refreshing authentication state");
                        _lastToken = currentToken;
                        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring token changes");
                }
            }
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
                        // Store the JWT token - JwtTokenHandler will pick it up automatically
                        _setToken(loginResponse.accessToken);
                        
                        _logger.LogInformation("Token stored successfully for {Email}", email);
                        
                        // Refresh auth state
                        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                        
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
                        // Store the JWT token - JwtTokenHandler will pick it up automatically
                        _setToken(loginResponse.accessToken);
                        
                        _logger.LogInformation("Ariba token stored successfully for session {SessionId}", sessionId);
                        
                        // Refresh auth state
                        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                        
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
            var token = _getToken();
            if (string.IsNullOrEmpty(token))
            {
                // No token = not authenticated, don't make API calls
                return new AuthenticationState(user);
            }

            try {
                // Token is automatically added by JwtTokenHandler
                var userResponse = await _httpClient.GetAsync("api/accounts/info");

                if (!userResponse.IsSuccessStatusCode)
                {
                    // Token might be expired or invalid
                    _setToken(null);
                    return new AuthenticationState(user);
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userJson, jsonSerializerOptions);

                if (userInfo != null) {
                    if (userInfo.ClientId == null) userInfo.ClientId = 0;
                    if (userInfo.ClientId == 0) userInfo.ClientName = "";

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userInfo.Email),
                        new(ClaimTypes.Email, userInfo.Email)
                    };

                    if (!string.IsNullOrEmpty(userInfo.GivenName))
                    {
                        claims.Add(new Claim(ClaimTypes.GivenName, userInfo.GivenName));
                        claims.Add(new Claim("FamilyName", userInfo.FamilyName ?? ""));
                    }

                    claims.Add(new Claim("ClientId", userInfo.ClientId.ToString()));
                    claims.Add(new Claim("ClientName", userInfo.ClientName ?? ""));

                    claims.AddRange(
                        userInfo.Claims.Where(c => c.Key != ClaimTypes.Name && c.Key != ClaimTypes.Email)
                            .Select(c => new Claim(c.Key, c.Value)));

                    // Get roles
                    var rolesResponse = await _httpClient.GetAsync("roles");
                    
                    if (rolesResponse.IsSuccessStatusCode)
                    {
                        var rolesJson = await rolesResponse.Content.ReadAsStringAsync();
                        var roles = JsonSerializer.Deserialize<RoleClaim[]>(rolesJson, jsonSerializerOptions);

                        if (roles?.Length > 0) {
                            foreach (var role in roles) {
                                if (!string.IsNullOrEmpty(role.Type) && !string.IsNullOrEmpty(role.Value)) {
                                    claims.Add(new Claim(role.Type, role.Value, role.ValueType, role.Issuer, role.OriginalIssuer));
                                }
                            }
                        }
                    }

                    var id = new ClaimsIdentity(claims, nameof(CookieAuthenticationStateProvider));
                    user = new ClaimsPrincipal(id);
                    _authenticated = true;
                }
            } catch { 
                // Clear token on any failure
                _setToken(null);
            }

            return new AuthenticationState(user);
        }

        public async Task LogoutAsync() {
            _setToken(null);

            const string Empty = "{}";
            var emptyContent = new StringContent(Empty, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("api/accounts/logout", emptyContent);  // Updated to use custom endpoint
            
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
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
