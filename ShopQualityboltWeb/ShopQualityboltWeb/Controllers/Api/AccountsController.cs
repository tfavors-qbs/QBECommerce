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

namespace ShopQualityboltWeb.Controllers.Api {
    [Route("api/accounts")]
    [ApiController]
    public class AccountsController : ControllerBase {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly IModelService<Client, ClientEditViewModel> _clientService;
		private readonly IModelService<PunchOutSession, PunchOutSession> _punchOutSessionService;

		public AccountsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config, IModelService<Client,ClientEditViewModel> clientService, IModelService<PunchOutSession, PunchOutSession> punchOutSessionService) {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _clientService = clientService;
            _punchOutSessionService = punchOutSessionService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] CustomRegisterRequest registerRequest) {
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
            return Ok(new RegisterResponse {
                Type = "Success",
                Title = "User registered successfully!",
                Status = 200,
                Detail = "The user was successfully registered.",
                Instance = HttpContext.Request.Path
            });
        }

		[HttpPost("login/ariba")]
		public async Task<ActionResult<LoginResponse>> Login([FromBody] string punchOutSessionId, bool useCookies = false)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(CreateErrorResponse("Validation Error", "Invalid request"));
			}

            var punchOutSession = _punchOutSessionService.Find(a => a.SessionId == punchOutSessionId).FirstOrDefault();
            if(punchOutSession == null)
            {
				return BadRequest(CreateErrorResponse("Validation Error", "No punch out session could be found for the punchOutSessionId"));
			}

            if (punchOutSession.ExpirationDateTime < DateTime.Now) 
            {
				return Unauthorized(CreateErrorResponse("Login Failed", "Punch out session expired"));
			}

            ApplicationUser user = _userManager.Users.FirstOrDefault(a => a.Id == punchOutSession.UserId);
			if (user == null)
			{
				return Unauthorized(CreateErrorResponse("Login Failed", "Could not find user associated with punch out session"));
			}
			
            await _signInManager.SignInAsync(user, true);

			if (useCookies)
			{
				return Ok(new { message = "Logged in with cookies" });
			}
			else
			{
				var token = GenerateJwtToken(user);
				int expiresIn = int.Parse(_config["Jwt:ExpireMinutes"]) * 60;
				var refreshToken = GenerateRefreshToken();

				return Ok(new LoginResponse
				{
					tokenType = "Bearer",
					accessToken = token,
					expiresIn = expiresIn,
					refreshToken = refreshToken
				});
			}
		}

		[HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest, bool useCookies = false) {
            if (!ModelState.IsValid) {
                return BadRequest(CreateErrorResponse("Validation Error", "Invalid request"));
            }
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null) {
                return Unauthorized(CreateErrorResponse("Login Failed", "Invalid email or password"));
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, false);
            if (!result.Succeeded) {
                return Unauthorized(CreateErrorResponse("Login Failed", "Invalid email or password."));
            }

            if (useCookies) {
                await _signInManager.SignInAsync(user, isPersistent: true);
                return Ok(new { message = "Logged in with cookies" });
            } else {
                var token = GenerateJwtToken(user);
                int expiresIn = int.Parse(_config["Jwt:ExpireMinutes"]) * 60;
                var refreshToken = GenerateRefreshToken();

                return Ok(new LoginResponse {
                    tokenType = "Bearer",
                    accessToken = token,
                    expiresIn = expiresIn,
                    refreshToken = refreshToken
                });
            }
        }

        [HttpGet("info")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetUserInfo() {
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
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.GivenName),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.FamilyName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
