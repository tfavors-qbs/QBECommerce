using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/coatings")]
    [ApiController]
    public class CoatingsApiController : ControllerBase {
        private readonly IModelService<Coating, Coating?> _service;
        private readonly IErrorLogService _errorLogService;

        public CoatingsApiController(IModelService<Coating, Coating?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Coating>>> GetCoatings() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Coating Error",
                    "Failed to Get Coatings",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve coatings" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Coating>> GetCoating(int id) {
            try {
                var coating = _service.GetById(id);

                if (coating == null) {
                    return NotFound();
                }

                return coating;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Coating Error",
                    "Failed to Get Coating",
                    ex.Message,
                    ex,
                    additionalData: new { coatingId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve coating" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutCoating(int id, Coating coating) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Coating Error",
                    "Failed to Update Coating",
                    ex.Message,
                    ex,
                    additionalData: new { coatingId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update coating" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Coating>> PostCoating(Coating coating) {
            try {
                if (_service.GetAll().Any(c => c.Name == coating.Name)) {
                    return Conflict("Coating with that name already exists.");
                }
                _service.Create(coating);

                return CreatedAtAction("GetCoating", new { id = coating.Id }, coating);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Coating Error",
                    "Failed to Create Coating",
                    ex.Message,
                    ex,
                    additionalData: new { name = coating?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create coating" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostCoatings([FromBody] List<Coating> coatings) {
            try {
                coatings = coatings.Where(c => !_service.GetAll().Any(c2 => c2.Name == c.Name)).ToList();
                _service.CreateRange(coatings);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Coating Error",
                    "Failed to Create Coatings Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = coatings?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create coatings" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCoating(int id) {
            try {
                var coating = _service.GetById(id);
                if (coating == null) {
                    return NotFound();
                }

                _service.Delete(coating);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Coating Error",
                    "Failed to Delete Coating",
                    ex.Message,
                    ex,
                    additionalData: new { coatingId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete coating" });
            }
        }

        private bool CoatingExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
