using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/specs")]
    [ApiController]
    public class SpecsApiController : ControllerBase {
        private readonly IModelService<Spec, Spec?> _service;

        public SpecsApiController(IModelService<Spec, Spec?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Spec>>> GetSpecs() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Spec>> GetSpec(int id) {
            var spec = _service.GetById(id);

            if (spec == null) {
                return NotFound();
            }

            return spec;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutSpec(int id, Spec spec) {
            if (id != spec.Id) {
                return BadRequest();
            }
            try {
                _service.Update(spec);
            } catch (DbUpdateConcurrencyException) {
                if (!SpecExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Spec>> PostSpec(Spec spec) {
            if (_service.GetAll().Any(s => s.Name == spec.Name)) {
                return Conflict("Spec with that name already exists.");
            }
            _service.Create(spec);

            return CreatedAtAction("GetSpec", new { id = spec.Id }, spec);
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostSpecs([FromBody] List<Spec> specs) {
            specs = specs.Where(s => !_service.GetAll().Any(s2 => s2.Name == s.Name)).ToList();
            _service.CreateRange(specs);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSpec(int id) {
            var spec = _service.GetById(id);
            if (spec == null) {
                return NotFound();
            }

            _service.Delete(spec);

            return NoContent();
        }

        private bool SpecExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
