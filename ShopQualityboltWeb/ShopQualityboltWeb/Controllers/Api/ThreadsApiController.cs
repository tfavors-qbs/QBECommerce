using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/threads")]
    [ApiController]
    public class ThreadsApiController : ControllerBase {
        private readonly IModelService<Thread, Thread?> _service;

        public ThreadsApiController(IModelService<Thread, Thread?> service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Thread>>> GetThreads() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Thread>> GetThread(int id) {
            var thread = _service.GetById(id);

            if (thread == null) {
                return NotFound();
            }

            return thread;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutThread(int id, Thread thread) {
            if (id != thread.Id) {
                return BadRequest();
            }
            try {
                _service.Update(thread);
            } catch (DbUpdateConcurrencyException) {
                if (!ThreadExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Thread>> PostThread(Thread thread) {
            if (_service.GetAll().Any(t => t.Name == thread.Name)) {
                return Conflict("Thread with that name already exists.");
            }
            _service.Create(thread);

            return CreatedAtAction("GetThread", new { id = thread.Id }, thread);
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostThreads([FromBody] List<Thread> threads) {
            threads = threads.Where(t => !_service.GetAll().Any(t2 => t2.Name == t.Name)).ToList();
            _service.CreateRange(threads);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteThread(int id) {
            var thread = _service.GetById(id);
            if (thread == null) {
                return NotFound();
            }

            _service.Delete(thread);

            return NoContent();
        }

        private bool ThreadExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
