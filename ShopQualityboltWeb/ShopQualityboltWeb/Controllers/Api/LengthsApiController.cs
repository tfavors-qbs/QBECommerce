using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/lengths")]
    [ApiController]
    public class LengthsApiController : ControllerBase {
        private readonly IModelService<Length, Length?> _service;

        public LengthsApiController(IModelService<Length, Length?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Length>>> GetLengths() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Length>> GetLength(int id) {
            var length = _service.GetById(id);

            if (length == null) {
                return NotFound();
            }

            return length;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutLength(int id, Length length) {
            if (id != length.Id) {
                return BadRequest();
            }
            try {
                _service.Update(length);
            } catch (DbUpdateConcurrencyException) {
                if (!LengthExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Length>> PostLength(Length length) {
            if (_service.GetAll().Any(l => l.Name ==  length.Name)) {
                Conflict("A length already exists with this name.");
            }
            _service.Create(length);

            return CreatedAtAction("GetLength", new { id = length.Id }, length);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostLength([FromBody] List<Length> lengths) {
            lengths = lengths.Where(l => !_service.GetAll().Any(l2 => l2.Name == l.Name)).ToList();
            _service.CreateRange(lengths);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLength(int id) {
            var length = _service.GetById(id);
            if (length == null) {
                return NotFound();
            }

            _service.Delete(length);

            return NoContent();
        }

        private bool LengthExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
