using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/materials")]
    [ApiController]
    public class MaterialsApiController : ControllerBase {
        private readonly IModelService<Material, Material?> _service;
        private readonly IErrorLogService _errorLogService;

        public MaterialsApiController(IModelService<Material, Material?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Material Error",
                    "Failed to Get Materials",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve materials" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> GetMaterial(int id) {
            try {
                var material = _service.GetById(id);

                if (material == null) {
                    return NotFound();
                }

                return material;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Material Error",
                    "Failed to Get Material",
                    ex.Message,
                    ex,
                    additionalData: new { materialId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve material" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutMaterial(int id, Material material) {
            try {
                if (id != material.Id) {
                    return BadRequest();
                }
                try {
                    _service.Update(material);
                } catch (DbUpdateConcurrencyException) {
                    if (!MaterialExists(id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Material Error",
                    "Failed to Update Material",
                    ex.Message,
                    ex,
                    additionalData: new { materialId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update material" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Material>> PostMaterial(Material material) {
            try {
                if (_service.GetAll().Any(m => m.Name == material.Name)) {
                    return Conflict("Material with that name already exists.");
                }
                _service.Create(material);

                return CreatedAtAction("GetMaterial", new { id = material.Id }, material);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Material Error",
                    "Failed to Create Material",
                    ex.Message,
                    ex,
                    additionalData: new { name = material?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create material" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostMaterials([FromBody] List<Material> material) {
            try {
                material = material.Where(m => !_service.GetAll().Any(m2 => m2.Name == m.Name)).ToList();
                _service.CreateRange(material);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Material Error",
                    "Failed to Create Materials Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = material?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create materials" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMaterial(int id) {
            try {
                var material = _service.GetById(id);
                if (material == null) {
                    return NotFound();
                }

                _service.Delete(material);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Material Error",
                    "Failed to Delete Material",
                    ex.Message,
                    ex,
                    additionalData: new { materialId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete material" });
            }
        }

        private bool MaterialExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
