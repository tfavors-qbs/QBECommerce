using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/classes")]
    [ApiController]
    public class ClassesApiController : ControllerBase {
        private readonly IModelService<Class, Class?> _service;

        public ClassesApiController(IModelService<Class, Class?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Class>>> GetClasses() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Class>> GetClass(int id) {
            var @class = _service.GetById(id);

            if (@class == null) {
                return NotFound();
            }

            return @class;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutClass(int id, Class @class) {
            if (id != @class.Id) {
                return BadRequest();
            }
            try {
                _service.Update(@class);
            } catch (DbUpdateConcurrencyException) {
                if (!ClassExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Class>> PostClass(Class @class) {
            if (_service.GetAll().Any(c => c.Name == @class.Name)) {
                return Conflict("Class with that name already exists.");
            }
            _service.Create(@class);

            return CreatedAtAction("GetClass", new { id = @class.Id }, @class);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostClasses([FromBody] List<Class> classes) {
            classes = classes.Where(c => !_service.GetAll().Any(c2 => c2.Name == c.Name)).ToList();
            _service.CreateRange(classes);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id) {
            var @class = _service.GetById(id);
            if (@class == null) {
                return NotFound();
            }

            _service.Delete(@class);

            return NoContent();
        }

        private bool ClassExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
