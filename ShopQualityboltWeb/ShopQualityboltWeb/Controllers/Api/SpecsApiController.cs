using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/specs")]
    [ApiController]
    public class SpecsApiController : ControllerBase {
        private readonly IModelService<Spec, Spec?> _service;
        private readonly IErrorLogService _errorLogService;

        public SpecsApiController(IModelService<Spec, Spec?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Spec>>> GetSpecs() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Spec Error",
                    "Failed to Get Specs",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve specs" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Spec>> GetSpec(int id) {
            try {
                var spec = _service.GetById(id);

                if (spec == null) {
                    return NotFound();
                }

                return spec;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Spec Error",
                    "Failed to Get Spec",
                    ex.Message,
                    ex,
                    additionalData: new { specId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve spec" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutSpec(int id, Spec spec) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Spec Error",
                    "Failed to Update Spec",
                    ex.Message,
                    ex,
                    additionalData: new { specId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update spec" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Spec>> PostSpec(Spec spec) {
            try {
                if (_service.GetAll().Any(s => s.Name == spec.Name)) {
                    return Conflict("Spec with that name already exists.");
                }
                _service.Create(spec);

                return CreatedAtAction("GetSpec", new { id = spec.Id }, spec);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Spec Error",
                    "Failed to Create Spec",
                    ex.Message,
                    ex,
                    additionalData: new { name = spec?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create spec" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostSpecs([FromBody] List<Spec> specs) {
            try {
                specs = specs.Where(s => !_service.GetAll().Any(s2 => s2.Name == s.Name)).ToList();
                _service.CreateRange(specs);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Spec Error",
                    "Failed to Create Specs Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = specs?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create specs" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSpec(int id) {
            try {
                var spec = _service.GetById(id);
                if (spec == null) {
                    return NotFound();
                }

                _service.Delete(spec);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Spec Error",
                    "Failed to Delete Spec",
                    ex.Message,
                    ex,
                    additionalData: new { specId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete spec" });
            }
        }

        private bool SpecExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
