using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/lengths")]
    [ApiController]
    public class LengthsApiController : ControllerBase {
        private readonly IModelService<Length, Length?> _service;
        private readonly IErrorLogService _errorLogService;

        public LengthsApiController(IModelService<Length, Length?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Length>>> GetLengths() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Length Error",
                    "Failed to Get Lengths",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve lengths" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Length>> GetLength(int id) {
            try {
                var length = _service.GetById(id);

                if (length == null) {
                    return NotFound();
                }

                return length;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Length Error",
                    "Failed to Get Length",
                    ex.Message,
                    ex,
                    additionalData: new { lengthId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve length" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutLength(int id, Length length) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Length Error",
                    "Failed to Update Length",
                    ex.Message,
                    ex,
                    additionalData: new { lengthId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update length" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Length>> PostLength(Length length) {
            try {
                if (_service.GetAll().Any(l => l.Name == length.Name)) {
                    return Conflict("A length already exists with this name.");
                }
                _service.Create(length);

                return CreatedAtAction("GetLength", new { id = length.Id }, length);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Length Error",
                    "Failed to Create Length",
                    ex.Message,
                    ex,
                    additionalData: new { name = length?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create length" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostLength([FromBody] List<Length> lengths) {
            try {
                lengths = lengths.Where(l => !_service.GetAll().Any(l2 => l2.Name == l.Name)).ToList();
                _service.CreateRange(lengths);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Length Error",
                    "Failed to Create Lengths Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = lengths?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create lengths" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLength(int id) {
            try {
                var length = _service.GetById(id);
                if (length == null) {
                    return NotFound();
                }

                _service.Delete(length);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Length Error",
                    "Failed to Delete Length",
                    ex.Message,
                    ex,
                    additionalData: new { lengthId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete length" });
            }
        }

        private bool LengthExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
