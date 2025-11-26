using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Services.Model;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Identity;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/contractitems")]
    [ApiController]
    public class ContractItemsApiController : ControllerBase {
        private readonly IModelService<ContractItem, ContractItemEditViewModel> _service;
        private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
		private readonly IErrorLogService _errorLogService;
		private readonly ILogger<ContractItemsApiController> _logger;

        public ContractItemsApiController(
			IModelService<ContractItem, ContractItemEditViewModel> service, 
			IModelMapper<ContractItem, ContractItemEditViewModel> mapper, 
			UserManager<ApplicationUser> userManager,
			IErrorLogService errorLogService,
			ILogger<ContractItemsApiController> logger) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
			_errorLogService = errorLogService;
			_logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItems() {
			try
			{
				var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null) return NotFound(new {message = "User not found."});
				if (user.ClientId == null || user.ClientId == 0) return _mapper.MapToEdit(_service.GetAll().Where(x => 0 == 1));
				return _mapper.MapToEdit(_service.GetAll().Where(x => x.ClientId == user.ClientId));
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Get Contract Items",
					ex.Message,
					ex,
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve contract items" });
			}
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Admin,QBSales")]
        public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItemsByClientForQBSales(int clientId) {
			try
			{
				var contractItems = _service.GetAll().Where(x => x.ClientId == clientId);
				return _mapper.MapToEdit(contractItems);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Get Contract Items by Client (QBSales)",
					ex.Message,
					ex,
					additionalData: new { clientId },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve contract items" });
			}
        }

        [HttpGet("admin/client/{clientId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItemsByClient(int clientId) {
			try
			{
				_logger.LogInformation("Getting contract items for client {ClientId}", clientId);
				var contractItems = _service.GetAll().Where(x => x.ClientId == clientId).ToList();
				_logger.LogInformation("Found {Count} contract items for client {ClientId}", contractItems.Count, clientId);
				
				// Debug: Check first few items before mapping
				foreach (var item in contractItems.Take(3)) {
					_logger.LogInformation("[API-Before-Map] Item {Id}: LengthId={LengthId}, Length={Length}, DiameterId={DiameterId}, Diameter={Diameter}",
						item.Id, item.LengthId, item.Length?.DisplayName ?? "NULL", item.DiameterId, item.Diameter?.DisplayName ?? "NULL");
				}

				var result = _mapper.MapToEdit(contractItems);
				_logger.LogInformation("Successfully mapped {Count} contract items", result.Count);
				
				// Debug: Check first few items after mapping
				foreach (var item in result.Take(3)) {
					_logger.LogInformation("[API-After-Map] Item {Id}: LengthId={LengthId}, LengthName={LengthName}, DiameterId={DiameterId}, DiameterName={DiameterName}",
						item.Id, item.LengthId, item.LengthName ?? "NULL", item.DiameterId, item.DiameterName ?? "NULL");
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get contract items for client {ClientId}. Error: {Message}", clientId, ex.Message);
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Get Contract Items by Client (Admin)",
					ex.Message,
					ex,
					additionalData: new { clientId },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve contract items" });
			}
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ContractItem>> GetContractItem(int id) {
			try
			{
				var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null) return NotFound(new {message = "User not found."});
				
				var contractItem = _service.GetById(id);

				if (contractItem == null) {
					return NotFound();
				}

				// Verify user has access to this contract item
				if (contractItem.ClientId != user.ClientId) {
					await _errorLogService.LogErrorAsync(
						"Contract Items Error",
						"Unauthorized Access to Contract Item",
						$"User attempted to access contract item {id} belonging to client {contractItem.ClientId}",
						additionalData: new { contractItemId = id, userClientId = user.ClientId, contractItemClientId = contractItem.ClientId },
						userId: user.Id,
						userEmail: user.Email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 403);
					return Forbid();
				}

				return contractItem;
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Get Contract Item",
					ex.Message,
					ex,
					additionalData: new { contractItemId = id },
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve contract item" });
			}
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutContractItem(int id, ContractItemEditViewModel contractItemEVM) {
			try
			{
				if (id != contractItemEVM.Id) {
					return BadRequest();
				}
				
				_service.Update(null, contractItemEVM);
				_logger.LogInformation("Updated contract item {Id}", id);
				return NoContent();
			} 
			catch (DbUpdateConcurrencyException ex) {
				if (!ContractItemExists(id)) {
					return NotFound();
				} else {
					await _errorLogService.LogErrorAsync(
						"Contract Items Error",
						"Concurrency Error Updating Contract Item",
						ex.Message,
						ex,
						additionalData: new { contractItemId = id },
						userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
						userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method);
					throw;
				}
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Update Contract Item",
					ex.Message,
					ex,
					additionalData: new { contractItemId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to update contract item" });
			}
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ContractItem>> PostContractItem(ContractItemEditViewModel contractItemEVM) {
			try
			{
				if (_service.GetAll().Any(ci => ci.CustomerStkNo == contractItemEVM.CustomerStkNo && ci.ClientId == contractItemEVM.ClientId)) {
					return Conflict("Contract already exists.");
				}
				var contractItem = _service.Create(null, contractItemEVM);
				_logger.LogInformation("Created contract item {Id} for client {ClientId}", contractItem.Id, contractItem.ClientId);

				return CreatedAtAction("GetContractItem", new { id = contractItem.Id }, contractItem);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Create Contract Item",
					ex.Message,
					ex,
					additionalData: new { customerStkNo = contractItemEVM?.CustomerStkNo, clientId = contractItemEVM?.ClientId },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to create contract item" });
			}
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostContractItems([FromBody] List<ContractItemEditViewModel> contractItemEVMs) {
			try
			{
				contractItemEVMs = contractItemEVMs.Where(ci => !_service.GetAll().Any(ci2 => ci2.CustomerStkNo == ci.CustomerStkNo
							&& ci2.ClientId == ci.ClientId)).ToList();
				_service.CreateRange(null, contractItemEVMs);
				_logger.LogInformation("Created {Count} contract items in bulk", contractItemEVMs.Count);
				return Ok();
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Create Contract Items in Bulk",
					ex.Message,
					ex,
					additionalData: new { itemCount = contractItemEVMs?.Count },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to create contract items" });
			}
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteContractItem(int id) {
			try
			{
				var contractItem = _service.GetById(id);
				if (contractItem == null) {
					return NotFound();
				}

				_service.Delete(contractItem);
				_logger.LogInformation("Deleted contract item {Id}", id);

				return NoContent();
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Contract Items Error",
					"Failed to Delete Contract Item",
					ex.Message,
					ex,
					additionalData: new { contractItemId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to delete contract item" });
			}
        }

        private bool ContractItemExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
