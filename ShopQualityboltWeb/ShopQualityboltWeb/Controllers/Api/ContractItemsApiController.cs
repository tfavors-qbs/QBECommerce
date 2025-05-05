using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Services.Model;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Identity;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/contractitems")]
    [ApiController]
    public class ContractItemsApiController : ControllerBase {
        private readonly IModelService<ContractItem, ContractItemEditViewModel> _service;
        private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContractItemsApiController(IModelService<ContractItem, ContractItemEditViewModel> service, IModelMapper<ContractItem, ContractItemEditViewModel> mapper, UserManager<ApplicationUser> userManager) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItems() {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new {message = "User not found."});
            if (user.ClientId == null || user.ClientId == 0) return _mapper.MapToEdit(_service.GetAll().Where(x => 0 == 1));
            return _mapper.MapToEdit(_service.GetAll().Where(x => x.ClientId == user.ClientId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContractItem>> GetContractItem(int id) {
            var contractItem = _service.GetById(id);

            if (contractItem == null) {
                return NotFound();
            }

            return contractItem;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutContractItem(int id, ContractItemEditViewModel contractItemEVM) {
            if (id != contractItemEVM.Id) {
                return BadRequest();
            }
            try {
                _service.Update(null, contractItemEVM);
            } catch (DbUpdateConcurrencyException) {
                if (!ContractItemExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<ContractItem>> PostContractItem(ContractItemEditViewModel contractItemEVM) {
            if (_service.GetAll().Any(ci => ci.CustomerStkNo == contractItemEVM.CustomerStkNo && ci.ClientId == contractItemEVM.ClientId)) {
                return Conflict("Contract already exists.");
            }
            var contractItem = _service.Create(null, contractItemEVM);

            return CreatedAtAction("GetContractItem", new { id = contractItem.Id }, contractItem);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostContractItems([FromBody] List<ContractItemEditViewModel> contractItemEVMs) {
            contractItemEVMs = contractItemEVMs.Where(ci => !_service.GetAll().Any(ci2 => ci2.CustomerStkNo == ci.CustomerStkNo
                        && ci2.ClientId == ci.ClientId)).ToList();
            _service.CreateRange(null, contractItemEVMs);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContractItem(int id) {
            var contractItem = _service.GetById(id);
            if (contractItem == null) {
                return NotFound();
            }

            _service.Delete(contractItem);

            return NoContent();
        }

        private bool ContractItemExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
