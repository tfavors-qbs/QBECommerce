using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models;
using System.Text.Json;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
	[Route("api/errorlogs")]
	[ApiController]
	public class ErrorLogsController : ControllerBase
	{
		private readonly DataContext _context;
		private readonly ILogger<ErrorLogsController> _logger;
		private readonly IConfiguration _configuration;
		private readonly IErrorLogService _errorLogService;

		public ErrorLogsController(DataContext context, ILogger<ErrorLogsController> logger, IConfiguration configuration, IErrorLogService errorLogService)
		{
			_context = context;
			_logger = logger;
			_configuration = configuration;
			_errorLogService = errorLogService;
		}

		/// <summary>
		/// Log an error from the client (e.g., PunchOut checkout error)
		/// </summary>
		[HttpPost]
		[AllowAnonymous] // Allow unauthenticated users to log errors
		public async Task<ActionResult<int>> LogError([FromBody] LogErrorRequest request)
		{
			try
			{
				var errorLog = new ErrorLog
				{
					ErrorType = request.ErrorType ?? "General Error",
					ErrorTitle = request.ErrorTitle ?? "Unknown Error",
					ErrorMessage = request.ErrorMessage ?? string.Empty,
					StackTrace = request.StackTrace,
					AdditionalData = request.AdditionalData != null 
						? JsonSerializer.Serialize(request.AdditionalData) 
						: null,
					UserId = User.Identity?.IsAuthenticated == true 
						? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
						: null,
					UserEmail = User.Identity?.IsAuthenticated == true 
						? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
						: null,
					RequestUrl = request.RequestUrl ?? HttpContext.Request.Path,
					HttpMethod = request.HttpMethod ?? HttpContext.Request.Method,
					IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
					UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
					SessionId = request.SessionId,
					StatusCode = request.StatusCode,
					Environment = _configuration["Environment"] ?? "Unknown",
					CreatedAt = DateTime.UtcNow
				};

				_context.ErrorLogs.Add(errorLog);
				await _context.SaveChangesAsync();

				// Also log to server logs for immediate visibility
				_logger.LogError("Error logged: {ErrorType} - {ErrorTitle}. ID: {ErrorId}", 
					errorLog.ErrorType, errorLog.ErrorTitle, errorLog.Id);

				return Ok(new { id = errorLog.Id, message = "Error logged successfully" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to log error to database");
				return StatusCode(500, "Failed to log error");
			}
		}

		/// <summary>
		/// Get all error logs with filtering and pagination
		/// </summary>
		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ErrorLogListResponse>> GetErrorLogs(
			[FromQuery] string? errorType = null,
			[FromQuery] bool? isResolved = null,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery] string? userId = null,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 50)
		{
			try
			{
				var query = _context.ErrorLogs.AsQueryable();

				// Apply filters
				if (!string.IsNullOrEmpty(errorType))
					query = query.Where(e => e.ErrorType == errorType);

				if (isResolved.HasValue)
					query = query.Where(e => e.IsResolved == isResolved.Value);

				if (startDate.HasValue)
					query = query.Where(e => e.CreatedAt >= startDate.Value);

				if (endDate.HasValue)
					query = query.Where(e => e.CreatedAt <= endDate.Value);

				if (!string.IsNullOrEmpty(userId))
					query = query.Where(e => e.UserId == userId);

				// Get total count before pagination
				var totalCount = await query.CountAsync();

				// Apply pagination
				var errors = await query
					.OrderByDescending(e => e.CreatedAt)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Include(e => e.User)
					.Select(e => new ErrorLogViewModel
					{
						Id = e.Id,
						ErrorType = e.ErrorType,
						ErrorTitle = e.ErrorTitle,
						ErrorMessage = e.ErrorMessage,
						StackTrace = e.StackTrace,
						AdditionalData = e.AdditionalData,
						UserId = e.UserId,
						UserEmail = e.UserEmail ?? (e.User != null ? e.User.Email : null),
						RequestUrl = e.RequestUrl,
						HttpMethod = e.HttpMethod,
						IpAddress = e.IpAddress,
						SessionId = e.SessionId,
						StatusCode = e.StatusCode,
						CreatedAt = e.CreatedAt,
						Environment = e.Environment,
						IsResolved = e.IsResolved,
						ResolvedAt = e.ResolvedAt,
						ResolvedBy = e.ResolvedBy,
						ResolutionNotes = e.ResolutionNotes
					})
					.ToListAsync();

				return Ok(new ErrorLogListResponse
				{
					Errors = errors,
					TotalCount = totalCount,
					Page = page,
					PageSize = pageSize,
					TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
				});
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Error Log System Error",
					"Failed to Retrieve Error Logs",
					ex.Message,
					ex,
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				_logger.LogError(ex, "Failed to retrieve error logs");
				return StatusCode(500, "Failed to retrieve error logs");
			}
		}

		/// <summary>
		/// Get a specific error log by ID
		/// </summary>
		[HttpGet("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ErrorLogViewModel>> GetErrorLog(int id)
		{
			try
			{
				var error = await _context.ErrorLogs
					.Include(e => e.User)
					.Where(e => e.Id == id)
					.Select(e => new ErrorLogViewModel
					{
						Id = e.Id,
						ErrorType = e.ErrorType,
						ErrorTitle = e.ErrorTitle,
						ErrorMessage = e.ErrorMessage,
						StackTrace = e.StackTrace,
						AdditionalData = e.AdditionalData,
						UserId = e.UserId,
						UserEmail = e.UserEmail ?? (e.User != null ? e.User.Email : null),
						RequestUrl = e.RequestUrl,
						HttpMethod = e.HttpMethod,
						IpAddress = e.IpAddress,
						SessionId = e.SessionId,
						StatusCode = e.StatusCode,
						CreatedAt = e.CreatedAt,
						Environment = e.Environment,
						IsResolved = e.IsResolved,
						ResolvedAt = e.ResolvedAt,
						ResolvedBy = e.ResolvedBy,
						ResolutionNotes = e.ResolutionNotes
					})
					.FirstOrDefaultAsync();

				if (error == null)
					return NotFound();

				return Ok(error);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Error Log System Error",
					"Failed to Retrieve Error Log",
					ex.Message,
					ex,
					additionalData: new { errorLogId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				_logger.LogError(ex, "Failed to retrieve error log {ErrorId}", id);
				return StatusCode(500, "Failed to retrieve error log");
			}
		}

		/// <summary>
		/// Mark an error as resolved
		/// </summary>
		[HttpPut("{id}/resolve")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult> ResolveError(int id, [FromBody] ResolveErrorRequest request)
		{
			try
			{
				var error = await _context.ErrorLogs.FindAsync(id);
				if (error == null)
					return NotFound();

				error.IsResolved = true;
				error.ResolvedAt = DateTime.UtcNow;
				error.ResolvedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				error.ResolutionNotes = request.ResolutionNotes;

				await _context.SaveChangesAsync();

				_logger.LogInformation("Error {ErrorId} resolved by {UserId}", id, error.ResolvedBy);

				return Ok(new { message = "Error marked as resolved" });
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Error Log System Error",
					"Failed to Resolve Error Log",
					ex.Message,
					ex,
					additionalData: new { errorLogId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				_logger.LogError(ex, "Failed to resolve error {ErrorId}", id);
				return StatusCode(500, "Failed to resolve error");
			}
		}

		/// <summary>
		/// Delete an error log
		/// </summary>
		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult> DeleteError(int id)
		{
			try
			{
				var error = await _context.ErrorLogs.FindAsync(id);
				if (error == null)
					return NotFound();

				_context.ErrorLogs.Remove(error);
				await _context.SaveChangesAsync();

				_logger.LogInformation("Error {ErrorId} deleted by {UserId}", 
					id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

				return Ok(new { message = "Error log deleted" });
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Error Log System Error",
					"Failed to Delete Error Log",
					ex.Message,
					ex,
					additionalData: new { errorLogId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				_logger.LogError(ex, "Failed to delete error {ErrorId}", id);
				return StatusCode(500, "Failed to delete error");
			}
		}

		/// <summary>
		/// Get error statistics
		/// </summary>
		[HttpGet("stats")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ErrorStatsResponse>> GetErrorStats([FromQuery] int days = 30)
		{
			try
			{
				var startDate = DateTime.UtcNow.AddDays(-days);

				var stats = new ErrorStatsResponse
				{
					TotalErrors = await _context.ErrorLogs.Where(e => e.CreatedAt >= startDate).CountAsync(),
					UnresolvedErrors = await _context.ErrorLogs.Where(e => !e.IsResolved && e.CreatedAt >= startDate).CountAsync(),
					ResolvedErrors = await _context.ErrorLogs.Where(e => e.IsResolved && e.CreatedAt >= startDate).CountAsync(),
					ErrorsByType = await _context.ErrorLogs
						.Where(e => e.CreatedAt >= startDate)
						.GroupBy(e => e.ErrorType)
						.Select(g => new ErrorTypeCount { ErrorType = g.Key, Count = g.Count() })
						.ToListAsync(),
					RecentErrors = await _context.ErrorLogs
						.Where(e => e.CreatedAt >= startDate)
						.OrderByDescending(e => e.CreatedAt)
						.Take(10)
						.Select(e => new ErrorSummary
						{
							Id = e.Id,
							ErrorType = e.ErrorType,
							ErrorTitle = e.ErrorTitle,
							CreatedAt = e.CreatedAt,
							IsResolved = e.IsResolved
						})
						.ToListAsync()
				};

				return Ok(stats);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Error Log System Error",
					"Failed to Retrieve Error Statistics",
					ex.Message,
					ex,
					additionalData: new { days },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				_logger.LogError(ex, "Failed to retrieve error stats");
				return StatusCode(500, "Failed to retrieve error stats");
			}
		}
	}

	// DTOs
	public class LogErrorRequest
	{
		public string? ErrorType { get; set; }
		public string? ErrorTitle { get; set; }
		public string? ErrorMessage { get; set; }
		public string? StackTrace { get; set; }
		public object? AdditionalData { get; set; }
		public string? RequestUrl { get; set; }
		public string? HttpMethod { get; set; }
		public string? SessionId { get; set; }
		public int? StatusCode { get; set; }
	}

	public class ResolveErrorRequest
	{
		public string? ResolutionNotes { get; set; }
	}

	public class ErrorLogListResponse
	{
		public List<ErrorLogViewModel> Errors { get; set; } = new();
		public int TotalCount { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
	}

	public class ErrorStatsResponse
	{
		public int TotalErrors { get; set; }
		public int UnresolvedErrors { get; set; }
		public int ResolvedErrors { get; set; }
		public List<ErrorTypeCount> ErrorsByType { get; set; } = new();
		public List<ErrorSummary> RecentErrors { get; set; } = new();
	}

	public class ErrorTypeCount
	{
		public string ErrorType { get; set; } = string.Empty;
		public int Count { get; set; }
	}

	public class ErrorSummary
	{
		public int Id { get; set; }
		public string ErrorType { get; set; } = string.Empty;
		public string ErrorTitle { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public bool IsResolved { get; set; }
	}
}
