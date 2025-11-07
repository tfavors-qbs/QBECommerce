using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models;

namespace ShopQualityboltWeb.Controllers.Api
{
    /// <summary>
    /// TEMPORARY controller for bootstrapping admin access.
    /// DELETE THIS FILE after granting admin access to at least one user.
    /// </summary>
    [Route("api/bootstrap")]
    [ApiController]
    public class BootstrapAdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public BootstrapAdminController(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Test endpoint to verify controller is working.
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> Health()
        {
            var dbContext = HttpContext.RequestServices.GetRequiredService<DataContext>();
            
            // Check if migrations are applied
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
            
            // Check if role tables exist
            bool rolesTableExists = false;
            try
            {
                var rolesCount = await _roleManager.Roles.CountAsync();
                rolesTableExists = true;
            }
            catch
            {
                rolesTableExists = false;
            }
            
            return Ok(new
            {
                status = "Bootstrap controller is active",
                timestamp = DateTime.UtcNow,
                environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
                bootstrapConfigured = !string.IsNullOrEmpty(_configuration["BootstrapSecret"]),
                database = new
                {
                    pendingMigrations = pendingMigrations.ToList(),
                    appliedMigrationsCount = appliedMigrations.Count(),
                    lastAppliedMigration = appliedMigrations.LastOrDefault(),
                    rolesTableExists = rolesTableExists
                }
            });
        }

        /// <summary>
        /// Grant Admin role to a user by email.
        /// TEMPORARY ENDPOINT - Remove this after initial setup!
        /// 
        /// Usage: POST /api/bootstrap/grant-admin
        /// Body: { "email": "your@email.com", "secret": "your-bootstrap-secret" }
        /// </summary>
        [HttpPost("grant-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> GrantAdminRole([FromBody] BootstrapRequest request)
        {
            // Simple secret check - set this in appsettings.json
            var bootstrapSecret = _configuration["BootstrapSecret"];
            
            if (string.IsNullOrEmpty(bootstrapSecret))
            {
                return BadRequest(new { error = "Bootstrap is not configured. Set BootstrapSecret in appsettings.json" });
            }

            if (request.Secret != bootstrapSecret)
            {
                return Unauthorized(new { error = "Invalid bootstrap secret" });
            }

            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            // Find the user
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(new { error = $"User with email '{request.Email}' not found" });
            }

            // Ensure Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Check if user already has admin role
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Ok(new 
                { 
                    message = $"User '{request.Email}' already has Admin role",
                    email = user.Email,
                    roles = await _userManager.GetRolesAsync(user)
                });
            }

            // Grant admin role
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            
            if (result.Succeeded)
            {
                return Ok(new 
                { 
                    message = $"Successfully granted Admin role to '{request.Email}'",
                    email = user.Email,
                    roles = await _userManager.GetRolesAsync(user)
                });
            }

            return BadRequest(new 
            { 
                error = "Failed to grant Admin role",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        /// <summary>
        /// List all users and their roles (for debugging).
        /// </summary>
        [HttpGet("list-users")]
        [AllowAnonymous]
        public async Task<IActionResult> ListUsers([FromQuery] string secret)
        {
            var bootstrapSecret = _configuration["BootstrapSecret"];
            
            if (string.IsNullOrEmpty(bootstrapSecret) || secret != bootstrapSecret)
            {
                return Unauthorized(new { error = "Invalid bootstrap secret" });
            }

            var users = _userManager.Users.ToList();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    email = user.Email,
                    givenName = user.GivenName,
                    familyName = user.FamilyName,
                    roles = roles
                });
            }

            return Ok(userList);
        }
    }

    public class BootstrapRequest
    {
        public string Email { get; set; }
        public string Secret { get; set; }
    }
}
