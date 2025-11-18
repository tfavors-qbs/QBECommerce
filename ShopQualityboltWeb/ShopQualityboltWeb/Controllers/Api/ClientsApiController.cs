using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using QBExternalWebLibrary.Data;
using Microsoft.AspNetCore.Identity;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsApiController : ControllerBase {
        private readonly IModelService<Client, ClientEditViewModel> _service;
        private readonly DataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientsApiController> _logger;

        public ClientsApiController(
            IModelService<Client, ClientEditViewModel> service, 
            DataContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ClientsApiController> logger) 
        {
            _service = service;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Client>> GetClient(int id) {
            var client = _service.GetById(id);

            if (client == null) {
                return NotFound();
            }

            return client;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutClient(int id, ClientEditViewModel clientEVM) {
            if (id != clientEVM.Id) {
                return BadRequest();
            }
            try {
                _service.Update(null, clientEVM);
            } catch (DbUpdateConcurrencyException) {
                if (!ClientExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Client>> PostClient(ClientEditViewModel clientEVM) {
            if (_service.GetAll().Any(c => c.LegacyId == clientEVM.LegacyId)) {
                return Conflict("A client already exists with the legacy id providied.");
            }
            var client = _service.Create(null, clientEVM);

            return CreatedAtAction("GetClient", new { id = client.Id }, client);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClient(int id) {
            var client = _service.GetById(id);
            if (client == null) {
                return NotFound(new { message = "Client not found" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Starting deletion of client {ClientId} - {ClientName}", id, client.Name);

                // Step 1: Delete all contract items associated with this client
                var contractItems = await _context.ContractItems
                    .Where(ci => ci.ClientId == id)
                    .ToListAsync();

                if (contractItems.Any())
                {
                    _context.ContractItems.RemoveRange(contractItems);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted {Count} contract items for client {ClientId}", contractItems.Count, id);
                }

                // Step 2: Disassociate all users from this client (set ClientId to null)
                var users = await _userManager.Users
                    .Where(u => u.ClientId == id)
                    .ToListAsync();

                if (users.Any())
                {
                    foreach (var user in users)
                    {
                        user.ClientId = null;
                        await _userManager.UpdateAsync(user);
                    }
                    _logger.LogInformation("Disassociated {Count} users from client {ClientId}", users.Count, id);
                }

                // Step 3: Delete the client
                _service.Delete(client);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                
                _logger.LogInformation("Successfully deleted client {ClientId} - {ClientName}", id, client.Name);

                return Ok(new ClientDeletionResponse
                {
                    Success = true,
                    Message = $"Client '{client.Name}' deleted successfully",
                    ClientId = id,
                    ClientName = client.Name,
                    DeletedContractItemsCount = contractItems.Count,
                    DisassociatedUsersCount = users.Count
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to delete client {ClientId}", id);
                
                return StatusCode(500, new ClientDeletionResponse
                {
                    Success = false,
                    Message = $"Failed to delete client: {ex.Message}",
                    ClientId = id,
                    ClientName = client.Name
                });
            }
        }

        private bool ClientExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }

    public class ClientDeletionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int DeletedContractItemsCount { get; set; }
        public int DisassociatedUsersCount { get; set; }
    }
}
