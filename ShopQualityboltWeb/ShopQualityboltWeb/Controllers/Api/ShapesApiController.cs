using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/shapes")]
    [ApiController]
    public class ShapesApiController : ControllerBase {
        private readonly IModelService<Shape, Shape?> _service;
        private readonly IErrorLogService _errorLogService;

        public ShapesApiController(IModelService<Shape, Shape?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shape>>> GetShapes() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Shape Error",
                    "Failed to Get Shapes",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve shapes" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shape>> GetShape(int id) {
            try {
                var shape = _service.GetById(id);

                if (shape == null) {
                    return NotFound();
                }

                return shape;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Shape Error",
                    "Failed to Get Shape",
                    ex.Message,
                    ex,
                    additionalData: new { shapeId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve shape" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutShape(int id, Shape shape) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Shape Error",
                    "Failed to Update Shape",
                    ex.Message,
                    ex,
                    additionalData: new { shapeId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update shape" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Shape>> PostShape(Shape shape) {
            try {
                if (_service.GetAll().Any(s => s.Name == shape.Name)) {
                    return Conflict("Shape with that name already exists.");
                }
                _service.Create(shape);

                return CreatedAtAction("GetShape", new { id = shape.Id }, shape);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Shape Error",
                    "Failed to Create Shape",
                    ex.Message,
                    ex,
                    additionalData: new { name = shape?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create shape" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostShapes([FromBody] List<Shape> shapes) {
            try {
                shapes = shapes.Where(s => !_service.GetAll().Any(s2 => s2.Name == s.Name)).ToList();
                _service.CreateRange(shapes);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Shape Error",
                    "Failed to Create Shapes Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = shapes?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create shapes" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteShape(int id) {
            try {
                var shape = _service.GetById(id);
                if (shape == null) {
                    return NotFound();
                }

                _service.Delete(shape);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Shape Error",
                    "Failed to Delete Shape",
                    ex.Message,
                    ex,
                    additionalData: new { shapeId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete shape" });
            }
        }

        private bool ShapeExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
