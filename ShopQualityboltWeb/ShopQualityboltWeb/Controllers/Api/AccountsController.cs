using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Ariba;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;
using QBExternalWebLibrary.Services.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CustomRegisterRequest = QBExternalWebLibrary.Services.Http.ContentTypes.Identity.CustomRegisterRequest;
using ShopQualityboltWeb.Services;

namespace ShopQualityboltWeb.Controllers.Api {
    [Route("api/accounts")]
    [ApiController]
    public class AccountsController : ControllerBase {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly IModelService<Client, ClientEditViewModel> _clientService;
		private readonly IModelService<PunchOutSession, PunchOutSession> _punchOutSessionService;
        private readonly ILogger<AccountsController> _logger;
		private readonly IErrorLogService _errorLogService;

		public AccountsController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            IConfiguration config, 
            IModelService<Client,ClientEditViewModel> clientService, 
            IModelService<PunchOutSession, PunchOutSession> punchOutSessionService,
            ILogger<AccountsController> logger,
			IErrorLogService errorLogService) {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _clientService = clientService;
            _punchOutSessionService = punchOutSessionService;
            _logger = logger;
			_errorLogService = errorLogService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] CustomRegisterRequest registerRequest) {
			try
			{
				if (!ModelState.IsValid) {
					return BadRequest(
						new RegisterResponse() {
							Type = "Error",
							Title = "Invalid Request",
							Status = 401,
							Detail = "Invalid Request",
							Instance = HttpContext.Request.Path,
							Errors = new Dictionary<string, List<string>> { { "Authentication", new List<string>() { "Invalid Request" } } }
						});
				}
				var user = new ApplicationUser() {
					UserName = registerRequest.email,
					Email = registerRequest.email,
					GivenName = registerRequest.givenName,
					FamilyName = registerRequest.familyName,
				};

				var result = await _userManager.CreateAsync(user, registerRequest.password);
				if (!result.Succeeded) {
					var errors = result.Errors
						.GroupBy(e => e.Code)
						.ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToList());
					
					await _errorLogService.LogErrorAsync(
						"Account Registration Error",
						"User Registration Failed",
						"Failed to create user account",
						additionalData: new { email = registerRequest.email, errors },
						userEmail: registerRequest.email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 400);

					return BadRequest(
						new RegisterResponse() {
							Type = "Error",
							Title = "Registration failed",
							Status = 401,
							Detail = "Registered failed",
							Instance = HttpContext.Request.Path,
							Errors = new Dictionary<string, List<string>> { { "Authentication", new List<string>() { "Registration failed" } } }
						});
				}
				
				_logger.LogInformation("User registered successfully: {Email}", registerRequest.email);
				
				return Ok(new RegisterResponse {
					Type = "Success",
					Title = "User registered successfully!",
					Status = 200,
					Detail = "The user was successfully registered.",
					Instance = HttpContext.Request.Path
				});
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Account Registration Error",
					"Unexpected Error During Registration",
					ex.Message,
					ex,
					additionalData: new { email = registerRequest?.email },
					userEmail: registerRequest?.email,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					statusCode: 500);

				_logger.LogError(ex, "Unexpected error during registration for email: {Email}", registerRequest?.email);
				return StatusCode(500, "An unexpected error occurred during registration");
			}
        }

		[HttpPost("login/ariba")]
		public async Task<ActionResult<LoginResponse>> LoginAriba([FromBody] string punchOutSessionId)
		{
			try
			{
				_logger?.LogInformation("[DEBUG API] LoginAriba: Received request with sessionId: {SessionId}", punchOutSessionId);
				
				if (!ModelState.IsValid)
				{
					_logger?.LogWarning("[DEBUG API] LoginAriba: ModelState is invalid");
					return BadRequest(CreateErrorResponse("Validation Error", "Invalid request"));
				}

				var punchOutSession = _punchOutSessionService.Find(a => a.SessionId == punchOutSessionId).FirstOrDefault();
				if(punchOutSession == null)
				{
					_logger?.LogWarning("[DEBUG API] LoginAriba: PunchOut session not found for sessionId: {SessionId}", punchOutSessionId);
					
					await _errorLogService.LogErrorAsync(
						"Ariba Login Error",
						"PunchOut Session Not Found",
						$"No PunchOut session found for session ID: {punchOutSessionId}",
						additionalData: new { sessionId = punchOutSessionId },
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						sessionId: punchOutSessionId,
						statusCode: 400);
					
					return BadRequest(CreateErrorResponse("Validation Error", "No punch out session could be found for the punchOutSessionId"));
				}

				_logger?.LogInformation("[DEBUG API] LoginAriba: PunchOut session found, UserId: {UserId}, Expires: {ExpirationDateTime}", 
					punchOutSession.UserId, punchOutSession.ExpirationDateTime);

				if (punchOutSession.ExpirationDateTime < DateTime.Now) 
				{
					_logger?.LogWarning("[DEBUG API] LoginAriba: PunchOut session expired");
					
					await _errorLogService.LogErrorAsync(
						"Ariba Login Error",
						"PunchOut Session Expired",
						$"PunchOut session {punchOutSessionId} expired at {punchOutSession.ExpirationDateTime}",
						additionalData: new { sessionId = punchOutSessionId, expirationDateTime = punchOutSession.ExpirationDateTime },
						userId: punchOutSession.UserId,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						sessionId: punchOutSessionId,
						statusCode: 401);
					
					return Unauthorized(CreateErrorResponse("Login Failed", "Punch out session expired"));
				}

				ApplicationUser user = _userManager.Users.FirstOrDefault(a => a.Id == punchOutSession.UserId);
				if (user == null)
				{
					_logger?.LogWarning("[DEBUG API] LoginAriba: User not found for UserId: {UserId}", punchOutSession.UserId);
					
					await _errorLogService.LogErrorAsync(
						"Ariba Login Error",
						"User Not Found for PunchOut Session",
						$"No user found with ID {punchOutSession.UserId} for session {punchOutSessionId}",
						additionalData: new { sessionId = punchOutSessionId, userId = punchOutSession.UserId },
						userId: punchOutSession.UserId,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						sessionId: punchOutSessionId,
						statusCode: 401);
					
					return Unauthorized(CreateErrorResponse("Login Failed", "Could not find user associated with punch out session"));
				}
				
				_logger?.LogInformation("[DEBUG API] LoginAriba: User found: {Email}, Generating JWT token...", user.Email);
				
				// For Ariba PunchOut, return JWT token instead of setting cookie
				// This works reliably in iframes across all browsers
				var token = GenerateJwtToken(user);
				int expiresIn = int.Parse(_config["Jwt:ExpireMinutes"]) * 60;

				_logger?.LogInformation("[DEBUG API] LoginAriba: Token generated, length: {Length}, expiresIn: {ExpiresIn}s", token.Length, expiresIn);
				_logger?.LogDebug("[DEBUG API] LoginAriba: Token preview: {TokenPreview}...", token.Substring(0, Math.Min(50, token.Length)));

				var response = new LoginResponse
				{
					tokenType = "Bearer",
					accessToken = token,
					expiresIn = expiresIn,
					refreshToken = punchOutSessionId // Use session ID as refresh token
				};

				_logger?.LogInformation("[DEBUG API] LoginAriba: Returning success response");
				return Ok(response);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Ariba Login Error",
					"Unexpected Error During Ariba Login",
					ex.Message,
					ex,
					additionalData: new { sessionId = punchOutSessionId },
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					sessionId: punchOutSessionId,
					statusCode: 500);

				_logger.LogError(ex, "Unexpected error during Ariba login for session: {SessionId}", punchOutSessionId);
				return StatusCode(500, "An unexpected error occurred during login");
			}
		}

		[HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest, bool useCookies = false) {
			try
			{
				if (!ModelState.IsValid) {
					return BadRequest(CreateErrorResponse("Validation Error", "Invalid request"));
				}
				
				var user = await _userManager.FindByEmailAsync(loginRequest.Email);
				if (user == null) {
					await _errorLogService.LogErrorAsync(
						"Login Error",
						"User Not Found",
						$"No user found with email: {loginRequest.Email}",
						additionalData: new { email = loginRequest.Email },
						userEmail: loginRequest.Email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 401);
					
					return Unauthorized(CreateErrorResponse("Login Failed", "Invalid email or password"));
				}
				
				var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, false);
				if (!result.Succeeded) {
					await _errorLogService.LogErrorAsync(
						"Login Error",
						"Invalid Password",
						$"Invalid password attempt for user: {loginRequest.Email}",
						additionalData: new { email = loginRequest.Email, isLockedOut = result.IsLockedOut, isNotAllowed = result.IsNotAllowed },
						userId: user.Id,
						userEmail: user.Email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 401);
					
					return Unauthorized(CreateErrorResponse("Login Failed", "Invalid email or password."));
				}

				// Always use JWT tokens now (more secure and consistent)
				_logger?.LogInformation("[DEBUG API] Regular login for user: {Email}, generating JWT token", user.Email);
				
				var token = GenerateJwtToken(user);
				int expiresIn = int.Parse(_config["Jwt:ExpireMinutes"]) * 60;
				var refreshToken = GenerateRefreshToken();

				_logger?.LogInformation("[DEBUG API] Token generated for {Email}, length: {Length}", user.Email, token.Length);

				return Ok(new LoginResponse {
					tokenType = "Bearer",
					accessToken = token,
					expiresIn = expiresIn,
					refreshToken = refreshToken
				});
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Login Error",
					"Unexpected Error During Login",
					ex.Message,
					ex,
					additionalData: new { email = loginRequest?.Email },
					userEmail: loginRequest?.Email,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					statusCode: 500);

				_logger.LogError(ex, "Unexpected error during login for email: {Email}", loginRequest?.Email);
				return StatusCode(500, "An unexpected error occurred during login");
			}
        }

        [HttpGet("info")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetUserInfo() {
			try
			{
				var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

				if (userId == null)
					return Unauthorized(new { message = "User is not authenticated." });

				var user = await _userManager.FindByIdAsync(userId);
				
				if (user == null)
					return NotFound(new { message = "User not found." });

				// Safely get client name
				string clientName = null;
				if (user.ClientId != null && user.ClientId > 0)
				{
					var client = _clientService.GetById(user.ClientId);
					clientName = client?.Name;
				}

				// Safely get roles
				var roles = await _userManager.GetRolesAsync(user);
				var rolesList = roles?.ToList() ?? new List<string>();

				UserInfo userInfo = new() {
					Email = user.Email ?? string.Empty,
					GivenName = user.GivenName ?? string.Empty,
					FamilyName = user.FamilyName ?? string.Empty,
					ClientId = user.ClientId,
					IsEmailConfirmed = user.EmailConfirmed,
					ClientName = clientName,
					Roles = rolesList
				};

				return Ok(userInfo);
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
				
				await _errorLogService.LogErrorAsync(
					"Account Info Error",
					"Failed to Get User Info",
					ex.Message,
					ex,
					userId: userId,
					userEmail: userEmail,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					statusCode: 500);

				_logger.LogError(ex, "Unexpected error getting user info for userId: {UserId}", userId);
				return StatusCode(500, "An unexpected error occurred");
			}
        }


        private ApiErrorResponse CreateErrorResponse(string title, string detail) {
            return new ApiErrorResponse {
                Type = "Error",
                Title = title,
                Status = 400,
                Detail = "One or more errors occurred during registration.",
                Instance = HttpContext.Request.Path,
                Errors = new Dictionary<string, List<string>> { { "Authentication", new List<string> { detail } } }
            };
        }

        private string GenerateJwtToken(ApplicationUser user) {
            var claims = new List<Claim> {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            // Add GivenName and FamilyName if they exist
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

            // Add ClientId if exists
            if (user.ClientId.HasValue)
            {
                claims.Add(new Claim("ClientId", user.ClientId.Value.ToString()));
            }

            // Add roles - CRITICAL for authorization
            var roles = _userManager.GetRolesAsync(user).Result;
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"]));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken() {
            // For demonstration, a simple GUID is used as a refresh token.
            // In production, you may want to store and manage this token in the database.
            return Guid.NewGuid().ToString();
        }

        public class ApiResponse {
            public string Type { get; set; }
            public string Title { get; set; }
            public int Status { get; set; }
            public string Detail { get; set; }
            public string Instance { get; set; }
        }

        public class ApiErrorResponse : ApiResponse {
            public Dictionary<string, List<string>> Errors { get; set; }
        }
    }
}
