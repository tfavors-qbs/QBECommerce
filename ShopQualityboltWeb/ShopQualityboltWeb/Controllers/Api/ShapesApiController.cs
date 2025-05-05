using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/shapes")]
    [ApiController]
    public class ShapesApiController : ControllerBase {
        private readonly IModelService<Shape, Shape?> _service;

        public ShapesApiController(IModelService<Shape, Shape?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shape>>> GetShapes() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shape>> GetShape(int id) {
            var shape = _service.GetById(id);

            if (shape == null) {
                return NotFound();
            }

            return shape;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutShape(int id, Shape shape) {
            if (id != shape.Id) {
                return BadRequest();
            }
            try {
                _service.Update(shape);
            } catch (DbUpdateConcurrencyException) {
                if (!ShapeExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Shape>> PostShape(Shape shape) {
            if (_service.GetAll().Any(s => s.Name == shape.Name)) {
                return Conflict("Shape with that name already exists.");
            }
            _service.Create(shape);

            return CreatedAtAction("GetShape", new { id = shape.Id }, shape);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostShapes([FromBody] List<Shape> shapes) {
            shapes = shapes.Where(s => !_service.GetAll().Any(s2 => s2.Name == s.Name)).ToList();
            _service.CreateRange(shapes);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShape(int id) {
            var shape = _service.GetById(id);
            if (shape == null) {
                return NotFound();
            }

            _service.Delete(shape);

            return NoContent();
        }

        private bool ShapeExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
