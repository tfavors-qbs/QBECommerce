using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/coatings")]
    [ApiController]
    public class CoatingsApiController : ControllerBase {
        private readonly IModelService<Coating, Coating?> _service;

        public CoatingsApiController(IModelService<Coating, Coating?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Coating>>> GetCoatings() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Coating>> GetCoating(int id) {
            var coating = _service.GetById(id);

            if (coating == null) {
                return NotFound();
            }

            return coating;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutCoating(int id, Coating coating) {
            if (id != coating.Id) {
                return BadRequest();
            }
            try {
                _service.Update(coating);
            } catch (DbUpdateConcurrencyException) {
                if (!CoatingExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Coating>> PostCoating(Coating coating) {
            if (_service.GetAll().Any(c => c.Name == coating.Name)) {
                return Conflict("Coating with that name already exists.");
            }
            _service.Create(coating);

            return CreatedAtAction("GetCoating", new { id = coating.Id }, coating);
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostCoatings([FromBody] List<Coating> coatings) {
            coatings = coatings.Where(c => !_service.GetAll().Any(c2 => c2.Name == c.Name)).ToList();
            _service.CreateRange(coatings);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCoating(int id) {
            var coating = _service.GetById(id);
            if (coating == null) {
                return NotFound();
            }

            _service.Delete(coating);

            return NoContent();
        }

        private bool CoatingExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
