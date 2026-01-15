using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/threads")]
    [ApiController]
    public class ThreadsApiController : ControllerBase {
        private readonly IModelService<Thread, Thread?> _service;
        private readonly IErrorLogService _errorLogService;

        public ThreadsApiController(IModelService<Thread, Thread?> service, IErrorLogService errorLogService) {
            _service = service;
            _errorLogService = errorLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Thread>>> GetThreads() {
            try {
                return _service.GetAll().ToList();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Thread Error",
                    "Failed to Get Threads",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve threads" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Thread>> GetThread(int id) {
            try {
                var thread = _service.GetById(id);

                if (thread == null) {
                    return NotFound();
                }

                return thread;
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Thread Error",
                    "Failed to Get Thread",
                    ex.Message,
                    ex,
                    additionalData: new { threadId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to retrieve thread" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutThread(int id, Thread thread) {
            try {
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
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Thread Error",
                    "Failed to Update Thread",
                    ex.Message,
                    ex,
                    additionalData: new { threadId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to update thread" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Thread>> PostThread(Thread thread) {
            try {
                if (_service.GetAll().Any(t => t.Name == thread.Name)) {
                    return Conflict("Thread with that name already exists.");
                }
                _service.Create(thread);

                return CreatedAtAction("GetThread", new { id = thread.Id }, thread);
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Thread Error",
                    "Failed to Create Thread",
                    ex.Message,
                    ex,
                    additionalData: new { name = thread?.Name },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create thread" });
            }
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostThreads([FromBody] List<Thread> threads) {
            try {
                threads = threads.Where(t => !_service.GetAll().Any(t2 => t2.Name == t.Name)).ToList();
                _service.CreateRange(threads);
                return Ok();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Thread Error",
                    "Failed to Create Threads Range",
                    ex.Message,
                    ex,
                    additionalData: new { count = threads?.Count },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to create threads" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteThread(int id) {
            try {
                var thread = _service.GetById(id);
                if (thread == null) {
                    return NotFound();
                }

                _service.Delete(thread);

                return NoContent();
            } catch (Exception ex) {
                await _errorLogService.LogErrorAsync(
                    "Thread Error",
                    "Failed to Delete Thread",
                    ex.Message,
                    ex,
                    additionalData: new { threadId = id },
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                return StatusCode(500, new { message = "Failed to delete thread" });
            }
        }

        private bool ThreadExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
