using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IErrorLogService _errorLogService;
		private readonly ILogger<UsersController> _logger;

        public UsersController(
			UserManager<ApplicationUser> userManager, 
			RoleManager<IdentityRole> roleManager,
			IErrorLogService errorLogService,
			ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
			_errorLogService = errorLogService;
			_logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserViewModel>>> GetUsers()
        {
			try
			{
				var users = await _userManager.Users
					.Include(u => u.Client)
					.ToListAsync();

				var userViewModels = new List<UserViewModel>();

				foreach (var user in users)
				{
					IList<string> roles;
					try
					{
						roles = await _userManager.GetRolesAsync(user);
					}
					catch (Exception ex)
					{
						roles = new List<string>();
						_logger.LogWarning(ex, "Error getting roles for user {Email}", user.Email);
					}
					
					userViewModels.Add(new UserViewModel
					{
						Id = user.Id,
						Email = user.Email ?? string.Empty,
						GivenName = user.GivenName ?? string.Empty,
						FamilyName = user.FamilyName ?? string.Empty,
						AribaId = user.AribaId ?? string.Empty,
						ClientId = user.ClientId,
						ClientName = user.Client?.Name,
						IsDisabled = user.IsDisabled,
						EmailConfirmed = user.EmailConfirmed,
						Roles = roles?.ToList() ?? new List<string>()
					});
				}

				return Ok(userViewModels);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"User Management Error",
					"Failed to Get Users List",
					ex.Message,
					ex,
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve users" });
			}
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserViewModel>> GetUser(string id)
        {
			try
			{
				var user = await _userManager.Users
					.Include(u => u.Client)
					.FirstOrDefaultAsync(u => u.Id == id);

				if (user == null)
				{
					return NotFound();
				}

				IList<string> roles;
				try
				{
					roles = await _userManager.GetRolesAsync(user);
				}
				catch (Exception ex)
				{
					roles = new List<string>();
					_logger.LogWarning(ex, "Error getting roles for user {Email}", user.Email);
				}

				var userViewModel = new UserViewModel
				{
					Id = user.Id,
					Email = user.Email ?? string.Empty,
					GivenName = user.GivenName ?? string.Empty,
					FamilyName = user.FamilyName ?? string.Empty,
					AribaId = user.AribaId ?? string.Empty,
					ClientId = user.ClientId,
					ClientName = user.Client?.Name,
					IsDisabled = user.IsDisabled,
					EmailConfirmed = user.EmailConfirmed,
					Roles = roles?.ToList() ?? new List<string>()
				};

				return Ok(userViewModel);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"User Management Error",
					"Failed to Get User",
					ex.Message,
					ex,
					additionalData: new { userId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve user" });
			}
        }

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetRoles()
        {
			try
			{
				var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
				return Ok(roles);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"User Management Error",
					"Failed to Get Roles",
					ex.Message,
					ex,
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve roles" });
			}
        }

        [HttpPost]
        public async Task<ActionResult<UserViewModel>> CreateUser([FromBody] CreateUserRequest request)
        {
			try
			{
				if (!ModelState.IsValid)
				{
					return BadRequest(ModelState);
				}

				var user = new ApplicationUser
				{
					UserName = request.Email,
					Email = request.Email,
					GivenName = request.GivenName,
					FamilyName = request.FamilyName,
					AribaId = request.AribaId,
					ClientId = request.ClientId,
					EmailConfirmed = true
				};

				var result = await _userManager.CreateAsync(user, request.Password);

				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description).ToList();
					await _errorLogService.LogErrorAsync(
						"User Management Error",
						"Failed to Create User",
						"User creation failed with validation errors",
						additionalData: new { email = request.Email, errors },
						userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
						userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method);
					return BadRequest(new { errors });
				}

				// Assign roles
				if (request.Roles != null && request.Roles.Any())
				{
					var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
					if (!roleResult.Succeeded)
					{
						var errors = roleResult.Errors.Select(e => e.Description).ToList();
						await _errorLogService.LogErrorAsync(
							"User Management Error",
							"Failed to Assign Roles to New User",
							"Role assignment failed",
							additionalData: new { email = request.Email, roles = request.Roles, errors },
							userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
							userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
							requestUrl: HttpContext.Request.Path,
							httpMethod: HttpContext.Request.Method);
						return BadRequest(new { errors });
					}
				}

				// Reload user with Client data
				var createdUser = await _userManager.Users
					.Include(u => u.Client)
					.FirstOrDefaultAsync(u => u.Id == user.Id);

				var roles = await _userManager.GetRolesAsync(createdUser);

				var userViewModel = new UserViewModel
				{
					Id = createdUser.Id,
					Email = createdUser.Email,
					GivenName = createdUser.GivenName,
					FamilyName = createdUser.FamilyName,
					AribaId = createdUser.AribaId,
					ClientId = createdUser.ClientId,
					ClientName = createdUser.Client?.Name,
					IsDisabled = createdUser.IsDisabled,
					EmailConfirmed = createdUser.EmailConfirmed,
					Roles = roles.ToList()
				};

				_logger.LogInformation("Created user {Email} with ID {UserId}", request.Email, user.Id);

				return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userViewModel);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"User Management Error",
					"Unexpected Error Creating User",
					ex.Message,
					ex,
					additionalData: new { email = request?.Email },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to create user" });
			}
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserViewModel>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
			try
			{
				if (!ModelState.IsValid)
				{
					return BadRequest(ModelState);
				}

				var user = await _userManager.FindByIdAsync(id);

				if (user == null)
				{
					return NotFound();
				}

				user.Email = request.Email;
				user.UserName = request.Email;
				user.GivenName = request.GivenName;
				user.FamilyName = request.FamilyName;
				user.AribaId = request.AribaId;
				user.ClientId = request.ClientId;
				user.IsDisabled = request.IsDisabled;

				var result = await _userManager.UpdateAsync(user);

				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description).ToList();
					await _errorLogService.LogErrorAsync(
						"User Management Error",
						"Failed to Update User",
						"User update failed with validation errors",
						additionalData: new { userId = id, email = request.Email, errors },
						userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
						userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method);
					return BadRequest(new { errors });
				}

				// Update password if provided
				if (!string.IsNullOrWhiteSpace(request.Password))
				{
					var token = await _userManager.GeneratePasswordResetTokenAsync(user);
					var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);

					if (!passwordResult.Succeeded)
					{
						var errors = passwordResult.Errors.Select(e => e.Description).ToList();
						await _errorLogService.LogErrorAsync(
							"User Management Error",
							"Failed to Update User Password",
							"Password reset failed",
							additionalData: new { userId = id, errors },
							userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
							userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
							requestUrl: HttpContext.Request.Path,
							httpMethod: HttpContext.Request.Method);
						return BadRequest(new { errors });
					}
				}

				// Update roles
				if (request.Roles != null)
				{
					var currentRoles = await _userManager.GetRolesAsync(user);
					var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
					
					if (!removeResult.Succeeded)
					{
						var errors = removeResult.Errors.Select(e => e.Description).ToList();
						await _errorLogService.LogErrorAsync(
							"User Management Error",
							"Failed to Remove User Roles",
							"Role removal failed",
							additionalData: new { userId = id, currentRoles, errors },
							userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
							userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
							requestUrl: HttpContext.Request.Path,
							httpMethod: HttpContext.Request.Method);
						return BadRequest(new { errors });
					}

					if (request.Roles.Any())
					{
						var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
						if (!addResult.Succeeded)
						{
							var errors = addResult.Errors.Select(e => e.Description).ToList();
							await _errorLogService.LogErrorAsync(
								"User Management Error",
								"Failed to Add User Roles",
								"Role assignment failed",
								additionalData: new { userId = id, newRoles = request.Roles, errors },
								userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
								userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
								requestUrl: HttpContext.Request.Path,
								httpMethod: HttpContext.Request.Method);
							return BadRequest(new { errors });
						}
					}
				}

				// Reload user with Client data
				var updatedUser = await _userManager.Users
					.Include(u => u.Client)
					.FirstOrDefaultAsync(u => u.Id == id);

				var roles = await _userManager.GetRolesAsync(updatedUser);

				var userViewModel = new UserViewModel
				{
					Id = updatedUser.Id,
					Email = updatedUser.Email,
					GivenName = updatedUser.GivenName,
					FamilyName = updatedUser.FamilyName,
					AribaId = updatedUser.AribaId,
					ClientId = updatedUser.ClientId,
					ClientName = updatedUser.Client?.Name,
					IsDisabled = updatedUser.IsDisabled,
					EmailConfirmed = updatedUser.EmailConfirmed,
					Roles = roles.ToList()
				};

				_logger.LogInformation("Updated user {Email} with ID {UserId}", request.Email, id);

				return Ok(userViewModel);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"User Management Error",
					"Unexpected Error Updating User",
					ex.Message,
					ex,
					additionalData: new { userId = id, email = request?.Email },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to update user" });
			}
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
			try
			{
				var user = await _userManager.FindByIdAsync(id);

				if (user == null)
				{
					return NotFound();
				}

				var userEmail = user.Email;
				var result = await _userManager.DeleteAsync(user);

				if (!result.Succeeded)
				{
					var errors = result.Errors.Select(e => e.Description).ToList();
					await _errorLogService.LogErrorAsync(
						"User Management Error",
						"Failed to Delete User",
						"User deletion failed",
						additionalData: new { userId = id, email = userEmail, errors },
						userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
						userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method);
					return BadRequest(new { errors });
				}

				_logger.LogInformation("Deleted user {Email} with ID {UserId}", userEmail, id);

				return NoContent();
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"User Management Error",
					"Unexpected Error Deleting User",
					ex.Message,
					ex,
					additionalData: new { userId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to delete user" });
			}
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? AribaId { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public bool IsDisabled { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? AribaId { get; set; }
        public int? ClientId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string? Password { get; set; } // Optional - only if changing password
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? AribaId { get; set; }
        public int? ClientId { get; set; }
        public bool IsDisabled { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
