using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/classes")]
    [ApiController]
    public class ClassesApiController : ControllerBase {
        private readonly IModelService<Class, Class?> _service;
        private readonly IErrorLogService _errorLogService;

        public ClassesApiController(IModelService<Class, Class?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Class>>> GetClasses() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Class Error",
                    "Failed to Get Classes",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve classes" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Class>> GetClass(int id) {
            try {
                var @class = _service.GetById(id);

                if (@class == null) {
                    return NotFound();
                }

                return @class;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Class Error",
                    "Failed to Get Class",
                    ex.Message,
                    ex,
                    additionalData: new { classId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve class" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutClass(int id, Class @class) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Class Error",
                    "Failed to Update Class",
                    ex.Message,
                    ex,
                    additionalData: new { classId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update class" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Class>> PostClass(Class @class) {
            try {
                if (_service.GetAll().Any(c => c.Name == @class.Name)) {
                    return Conflict("Class with that name already exists.");
                }
                _service.Create(@class);

                return CreatedAtAction("GetClass", new { id = @class.Id }, @class);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Class Error",
                    "Failed to Create Class",
                    ex.Message,
                    ex,
                    additionalData: new { name = @class?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create class" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostClasses([FromBody] List<Class> classes) {
            try {
                classes = classes.Where(c => !_service.GetAll().Any(c2 => c2.Name == c.Name)).ToList();
                _service.CreateRange(classes);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Class Error",
                    "Failed to Create Classes Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = classes?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create classes" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClass(int id) {
            try {
                var @class = _service.GetById(id);
                if (@class == null) {
                    return NotFound();
                }

                _service.Delete(@class);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Class Error",
                    "Failed to Delete Class",
                    ex.Message,
                    ex,
                    additionalData: new { classId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete class" });
            }
        }

        private bool ClassExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
