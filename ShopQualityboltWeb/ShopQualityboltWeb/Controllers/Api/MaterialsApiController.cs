using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/materials")]
    [ApiController]
    public class MaterialsApiController : ControllerBase {
        private readonly IModelService<Material, Material?> _service;

        public MaterialsApiController(IModelService<Material, Material?> service) {
            _service = service;
        }

        // GET: api/MaterialsApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials() {
            return _service.GetAll().ToList();
        }

        // GET: api/MaterialsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> GetMaterial(int id) {
            var material = _service.GetById(id);

            if (material == null) {
                return NotFound();
            }

            return material;
        }

        // PUT: api/MaterialsApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaterial(int id, Material material) {
            if (id != material.Id) {
                return BadRequest();
            }

            //_context.Entry(material).State = EntityState.Modified;

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
        }

        // POST: api/MaterialsApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Material>> PostMaterial(Material material) {
            if (_service.GetAll().Any(m => m.Name == material.Name)) {
                return Conflict("Material with that name already exists.");
            }
            _service.Create(material);

            return CreatedAtAction("GetMaterial", new { id = material.Id }, material);
        }

        [HttpPost("range")]
        public async Task<ActionResult> PostMaterials([FromBody] List<Material> material) {
            material = material.Where(m => !_service.GetAll().Any(m2 => m2.Name == m.Name)).ToList();
            _service.CreateRange(material);
            return Ok();
        }

        // DELETE: api/MaterialsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id) {
            var material = _service.GetById(id);
            if (material == null) {
                return NotFound();
            }

            _service.Delete(material);

            return NoContent();
        }

        private bool MaterialExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
