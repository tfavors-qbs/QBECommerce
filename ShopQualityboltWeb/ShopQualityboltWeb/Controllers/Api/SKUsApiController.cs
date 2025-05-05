using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using Microsoft.AspNetCore.Authorization;
using QBExternalWebLibrary.Services.Model;
using QBExternalWebLibrary.Models.Mapping;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/skus")]
    [ApiController]
    public class SKUsApiController : ControllerBase {
        private readonly IModelService<SKU, SKUEditViewModel> _service;
        private readonly IModelMapper<SKU, SKUEditViewModel> _mapper;

        public SKUsApiController(IModelService<SKU, SKUEditViewModel> service, IModelMapper<SKU,SKUEditViewModel> mapper) {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SKUEditViewModel>>> GetSKUs() {
            return _mapper.MapToEdit(_service.GetAll());
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SKU>> GetSKU(int id) {
            var sku = _service.GetById(id);

            if (sku == null) {
                return NotFound();
            }

            return sku;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutSKU(int id, SKUEditViewModel skuEVM) {
             if (id != skuEVM.Id) {
                return BadRequest();
            }
            try {
                _service.Update(null, skuEVM);
            } catch (DbUpdateConcurrencyException) {
                if (!SKUExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<SKU>> PostSKU(SKUEditViewModel skuEVM) {
            if (_service.GetAll().Any(s => s.Name == skuEVM.Name)) {
                return Conflict("SKU with that name already exists.");
            }
            var sku = _service.Create(null, skuEVM);

            return CreatedAtAction("GetSKU", new { id = sku.Id }, sku);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostSKUs([FromBody] List<SKUEditViewModel> skuEVMs) {
            skuEVMs = skuEVMs.Where(s => !_service.GetAll().Any(s2 => s2.Name == s.Name)).ToList();
            _service.CreateRange(null, skuEVMs);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSKU (int id) {
            var sku = _service.GetById(id);
            if (sku == null) {
                return NotFound();
            }

            _service.Delete(sku);

            return NoContent();
        }

        private bool SKUExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
