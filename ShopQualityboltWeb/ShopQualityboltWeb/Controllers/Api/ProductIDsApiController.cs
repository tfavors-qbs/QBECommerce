using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/productids")]
    [ApiController]
    public class ProductIDsApiController : ControllerBase {
        private readonly IModelService<ProductID, ProductIDEditViewModel> _service;

        public ProductIDsApiController(IModelService<ProductID, ProductIDEditViewModel> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductID>>> GetProductIDs() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductID>> GetProductID(int id) {
            var productID = _service.GetById(id);

            if (productID == null) {
                return NotFound();
            }

            return productID;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProductID(int id, ProductIDEditViewModel productIDEVM) {
            if (id != productIDEVM.Id) {
                return BadRequest();
            }
            try {
                _service.Update(null, productIDEVM);
            } catch (DbUpdateConcurrencyException) {
                if (!ProductIDExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductID>> PostProductID(ProductIDEditViewModel productIDEVM) {
            if (_service.GetAll().Any(p => p.LegacyName == productIDEVM.LegacyName)) {
                return Conflict("ProductID with that name already exists.");
            }
            var productID = _service.Create(null, productIDEVM);

            return CreatedAtAction("GetProductID", new { id = productID.Id }, productID);
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostProductIDs([FromBody] List<ProductIDEditViewModel> productIDEVMs) {
            productIDEVMs = productIDEVMs.Where(p => !_service.GetAll().Any(p2 => p2.LegacyName == p.LegacyName)).ToList();
            _service.CreateRange(null, productIDEVMs);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductID(int id) {
            var productID = _service.GetById(id);
            if (productID == null) {
                return NotFound();
            }

            _service.Delete(productID);

            return NoContent();
        }

        private bool ProductIDExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
