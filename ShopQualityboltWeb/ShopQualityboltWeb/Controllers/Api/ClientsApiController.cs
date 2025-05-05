using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsApiController : ControllerBase {
        private readonly IModelService<Client, ClientEditViewModel> _service;

        public ClientsApiController(IModelService<Client, ClientEditViewModel> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(int id) {
            var client = _service.GetById(id);

            if (client == null) {
                return NotFound();
            }

            return client;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(int id, ClientEditViewModel clientEVM) {
            if (id != clientEVM.Id) {
                return BadRequest();
            }
            try {
                _service.Update(null, clientEVM);
            } catch (DbUpdateConcurrencyException) {
                if (!ClientExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Client>> PostClient(ClientEditViewModel clientEVM) {
            if (_service.GetAll().Any(c => c.LegacyId == clientEVM.LegacyId)) {
                return Conflict("A client already exists with the legacy id providied.");
            }
            var client = _service.Create(null, clientEVM);

            return CreatedAtAction("GetClient", new { id = client.Id }, client);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id) {
            var client = _service.GetById(id);
            if (client == null) {
                return NotFound();
            }

            _service.Delete(client);

            return NoContent();
        }

        private bool ClientExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
