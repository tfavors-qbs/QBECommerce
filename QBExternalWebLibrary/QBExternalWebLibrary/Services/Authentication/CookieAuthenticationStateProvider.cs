using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;
using System.Net.Http.Headers;

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
        /// <param name="getToken">Function to get current JWT token.</param>
        /// <param name="setToken">Action to set JWT token.</param>
        public CookieAuthenticationStateProvider(
            IHttpClientFactory httpClientFactory,
            Func<string?> getToken,
            Action<string?> setToken)
        {
            _httpClient = httpClientFactory.CreateClient("Auth");
            _getToken = getToken;
            _setToken = setToken;
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
                Console.WriteLine($"[JWT AUTH] Starting regular login for email: {email}");
                
                var result = await _httpClient.PostAsJsonAsync(
                    "api/accounts/login", new {
                        email,
                        password
                    });

                Console.WriteLine($"[JWT AUTH] Regular login response status: {result.StatusCode}");

                if (result.IsSuccessStatusCode) {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"[JWT AUTH] Response: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");
                    
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, jsonSerializerOptions);
                    
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.accessToken))
                    {
                        // Store the JWT token - JwtTokenHandler will pick it up automatically
                        _setToken(loginResponse.accessToken);
                        
                        Console.WriteLine($"[JWT AUTH] Token stored successfully for {email}. Length: {loginResponse.accessToken.Length}");
                        
                        // Refresh auth state
                        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                        
                        return new FormResult { Succeeded = true };
                    }
                    else
                    {
                        Console.WriteLine($"[JWT AUTH] Login response or token was null/empty");
                    }
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"[JWT AUTH] Regular login failed: {result.StatusCode} - {errorContent}");
                }

                return new FormResult {
                    Succeeded = false,
                    ErrorList = ["Invalid email and/or password."]
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JWT AUTH] Regular login exception: {ex.Message}");
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
                Console.WriteLine($"[JWT AUTH] Starting Ariba login with sessionId: {sessionId}");
                
                var result = await _httpClient.PostAsJsonAsync("api/accounts/login/ariba", sessionId);

                Console.WriteLine($"[JWT AUTH] Login response status: {result.StatusCode}");

                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"[JWT AUTH] Response: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");
                    
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, jsonSerializerOptions);
                    
                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.accessToken))
                    {
                        // Store the JWT token - JwtTokenHandler will pick it up automatically
                        _setToken(loginResponse.accessToken);
                        
                        Console.WriteLine($"[JWT AUTH] Token stored successfully. Length: {loginResponse.accessToken.Length}");
                        Console.WriteLine($"[JWT AUTH] Token verification: {(_getToken() != null ? "SUCCESS" : "FAILED")}");
                        
                        // Refresh auth state
                        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                        
                        return new FormResult { Succeeded = true };
                    }
                    else
                    {
                        Console.WriteLine($"[JWT AUTH] Login response or token was null/empty");
                    }
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"[JWT AUTH] Login failed: {result.StatusCode} - {errorContent}");
                }

                return new FormResult
                {
                    Succeeded = false,
                    ErrorList = ["Failed to authenticate with PunchOut session."]
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JWT AUTH] Exception: {ex.Message}");
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
            Console.WriteLine($"[JWT AUTH] GetAuthenticationStateAsync called. Token present: {(_getToken() != null)}");
            
            _authenticated = false;
            var user = Unauthenticated;

            try {
                // Token is automatically added by JwtTokenHandler
                var userResponse = await _httpClient.GetAsync("api/accounts/info");

                Console.WriteLine($"[JWT AUTH] api/accounts/info response: {userResponse.StatusCode}");

                userResponse.EnsureSuccessStatusCode();

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
                    rolesResponse.EnsureSuccessStatusCode();

                    var rolesJson = await rolesResponse.Content.ReadAsStringAsync();
                    var roles = JsonSerializer.Deserialize<RoleClaim[]>(rolesJson, jsonSerializerOptions);

                    if (roles?.Length > 0) {
                        foreach (var role in roles) {
                            if (!string.IsNullOrEmpty(role.Type) && !string.IsNullOrEmpty(role.Value)) {
                                claims.Add(new Claim(role.Type, role.Value, role.ValueType, role.Issuer, role.OriginalIssuer));
                            }
                        }
                    }

                    var id = new ClaimsIdentity(claims, nameof(CookieAuthenticationStateProvider));
                    user = new ClaimsPrincipal(id);
                    _authenticated = true;
                    Console.WriteLine($"[JWT AUTH] User authenticated successfully: {userInfo.Email}");
                }
            } catch (Exception ex) { 
                Console.WriteLine($"[JWT AUTH] Authentication failed: {ex.Message}");
                _setToken(null);
            }

            return new AuthenticationState(user);
        }

        public async Task LogoutAsync() {
            _setToken(null);

            const string Empty = "{}";
            var emptyContent = new StringContent(Empty, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("logout", emptyContent);
            
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
