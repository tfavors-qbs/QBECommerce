using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/groups")]
    [ApiController]
    public class GroupsApiController : ControllerBase {
        private readonly IModelService<Group, GroupEditViewModel> _service;

        public GroupsApiController(IModelService<Group, GroupEditViewModel> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Group>>> GetGroups() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Group>> GetGroup(int id) {
            var group = _service.GetById(id);

            if (group == null) {
                return NotFound();
            }

            return group;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutGroup(int id, GroupEditViewModel groupEVM) {
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
        }

        [HttpPost]
        public async Task<ActionResult<Group>> PostGroup(GroupEditViewModel groupEVM) {
            if (_service.GetAll().Any(g => g.LegacyId == groupEVM.LegacyId && g.ClassId == groupEVM.ClassId)) {
                return Conflict("Group with that legacy id already exists.");
            }
            var group = _service.Create(null, groupEVM);

            return CreatedAtAction("GetGroup", new { id = group.Id }, group);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostGroups([FromBody] List<GroupEditViewModel> groupEVMs) {
            groupEVMs = groupEVMs.Where(g => !_service.GetAll().Any(g2 => g2.ClassId == g.ClassId && g2.LegacyId == g.LegacyId)).ToList();
            _service.CreateRange(null, groupEVMs);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id) {
            var group = _service.GetById(id);
            if (group == null) {
                return NotFound();
            }

            _service.Delete(group);

            return NoContent();
        }

        private bool GroupExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
