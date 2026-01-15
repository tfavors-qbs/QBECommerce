using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api {
    [Route("api/diameters")]
    [ApiController]
    public class DiametersApiController : ControllerBase {
        private readonly IModelService<Diameter, Diameter?> _service;
        private readonly IErrorLogService _errorLogService;

        public DiametersApiController(IModelService<Diameter, Diameter?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Diameter>>> GetDiameters() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Diameter Error",
                    "Failed to Get Diameters",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve diameters" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Diameter>> GetDiameter(int id) {
            try {
                var diameter = _service.GetById(id);

                if (diameter == null) {
                    return NotFound();
                }

                return diameter;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Diameter Error",
                    "Failed to Get Diameter",
                    ex.Message,
                    ex,
                    additionalData: new { diameterId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve diameter" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutDiameter(int id, Diameter diameter) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Diameter Error",
                    "Failed to Update Diameter",
                    ex.Message,
                    ex,
                    additionalData: new { diameterId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update diameter" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Diameter>> PostDiameter(Diameter diameter) {
            try {
                if (_service.GetAll().Any(d => d.Name == diameter.Name)) {
                    return Conflict("Diameter with that name already exists.");
                }
                _service.Create(diameter);

                return CreatedAtAction("GetDiameter", new { id = diameter.Id }, diameter);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Diameter Error",
                    "Failed to Create Diameter",
                    ex.Message,
                    ex,
                    additionalData: new { name = diameter?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create diameter" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostDiameter([FromBody] List<Diameter> diameters) {
            try {
                diameters = diameters.Where(d => !_service.GetAll().Any(d2 => d2.Name == d.Name)).ToList();
                _service.CreateRange(diameters);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Diameter Error",
                    "Failed to Create Diameters Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = diameters?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create diameters" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDiameter(int id) {
            try {
                var diameter = _service.GetById(id);
                if (diameter == null) {
                    return NotFound();
                }

                _service.Delete(diameter);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Diameter Error",
                    "Failed to Delete Diameter",
                    ex.Message,
                    ex,
                    additionalData: new { diameterId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete diameter" });
            }
        }

        private bool DiameterExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
