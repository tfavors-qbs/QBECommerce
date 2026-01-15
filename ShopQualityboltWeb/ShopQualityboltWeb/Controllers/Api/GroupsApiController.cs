using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/groups")]
    [ApiController]
    public class GroupsApiController : ControllerBase {
        private readonly IModelService<Group, GroupEditViewModel> _service;
        private readonly IErrorLogService _errorLogService;

        public GroupsApiController(IModelService<Group, GroupEditViewModel> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Group>>> GetGroups() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Group Error",
                    "Failed to Get Groups",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve groups" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Group>> GetGroup(int id) {
            try {
                var group = _service.GetById(id);

                if (group == null) {
                    return NotFound();
                }

                return group;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Group Error",
                    "Failed to Get Group",
                    ex.Message,
                    ex,
                    additionalData: new { groupId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve group" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutGroup(int id, GroupEditViewModel groupEVM) {
            try {
                if (id != groupEVM.Id) {
                    return BadRequest();
                }
                try {
                    _service.Update(null, groupEVM);
                } catch (DbUpdateConcurrencyException) {
                    if (!GroupExists(id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Group Error",
                    "Failed to Update Group",
                    ex.Message,
                    ex,
                    additionalData: new { groupId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update group" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Group>> PostGroup(GroupEditViewModel groupEVM) {
            try {
                if (_service.GetAll().Any(g => g.LegacyId == groupEVM.LegacyId && g.ClassId == groupEVM.ClassId)) {
                    return Conflict("Group with that legacy id already exists.");
                }
                var group = _service.Create(null, groupEVM);

                return CreatedAtAction("GetGroup", new { id = group.Id }, group);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Group Error",
                    "Failed to Create Group",
                    ex.Message,
                    ex,
                    additionalData: new { legacyId = groupEVM?.LegacyId },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create group" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostGroups([FromBody] List<GroupEditViewModel> groupEVMs) {
            try {
                groupEVMs = groupEVMs.Where(g => !_service.GetAll().Any(g2 => g2.ClassId == g.ClassId && g2.LegacyId == g.LegacyId)).ToList();
                _service.CreateRange(null, groupEVMs);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Group Error",
                    "Failed to Create Groups Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = groupEVMs?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create groups" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGroup(int id) {
            try {
                var group = _service.GetById(id);
                if (group == null) {
                    return NotFound();
                }

                _service.Delete(group);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Group Error",
                    "Failed to Delete Group",
                    ex.Message,
                    ex,
                    additionalData: new { groupId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete group" });
            }
        }

        private bool GroupExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
