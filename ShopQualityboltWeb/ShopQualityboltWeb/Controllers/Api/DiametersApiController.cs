using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;

namespace ShopQualityboltWeb.Controllers.Api {
    [Route("api/diameters")]
    [ApiController]
    public class DiametersApiController : ControllerBase {
        private readonly IModelService<Diameter, Diameter?> _service;

        public DiametersApiController(IModelService<Diameter, Diameter?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Diameter>>> GetDiameters() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Diameter>> GetDiameter(int id) {
            var diameter = _service.GetById(id);

            if (diameter == null) {
                return NotFound();
            }

            return diameter;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutDiameter(int id, Diameter diameter) {
            if (id != diameter.Id) {
                return BadRequest();
            }
            try {
                _service.Update(diameter);
            } catch (DbUpdateConcurrencyException) {
                if (!DiameterExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Diameter>> PostDiameter(Diameter diameter) {
            if (_service.GetAll().Any(d => d.Name == diameter.Name)) {
                return Conflict("Diameter with that name already exists.");
            }
            _service.Create(diameter);

            return CreatedAtAction("GetDiameter", new { id = diameter.Id }, diameter);
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostDiameter([FromBody] List<Diameter> diameters) {
            diameters = diameters.Where(d => !_service.GetAll().Any(d2 => d2.Name == d.Name)).ToList();
            _service.CreateRange(diameters);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDiameter(int id) {
            var diameter = _service.GetById(id);
            if (diameter == null) {
                return NotFound();
            }

            _service.Delete(diameter);

            return NoContent();
        }

        private bool DiameterExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
