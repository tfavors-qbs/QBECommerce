using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserViewModel>>> GetUsers()
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
                    // If roles table doesn't exist, return empty list
                    roles = new List<string>();
                    Console.WriteLine($"Error getting roles for user {user.Email}: {ex.Message}");
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

        [HttpGet("{id}")]
        public async Task<ActionResult<UserViewModel>> GetUser(string id)
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
                // If roles table doesn't exist or other error, return empty list
                // This allows the UI to work even if migrations haven't run
                roles = new List<string>();
                // Log the error for debugging
                Console.WriteLine($"Error getting roles for user {user.Email}: {ex.Message}");
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

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetRoles()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<ActionResult<UserViewModel>> CreateUser([FromBody] CreateUserRequest request)
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
                EmailConfirmed = true // Auto-confirm for admin-created users
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { errors });
            }

            // Assign roles
            if (request.Roles != null && request.Roles.Any())
            {
                var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors.Select(e => e.Description).ToList();
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

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userViewModel);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserViewModel>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
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
                    return BadRequest(new { errors });
                }

                if (request.Roles.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
                    if (!addResult.Succeeded)
                    {
                        var errors = addResult.Errors.Select(e => e.Description).ToList();
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

            return Ok(userViewModel);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { errors });
            }

            return NoContent();
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
