using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models;
using System.Text.Json;

namespace ShopQualityboltWeb.Services
{
	public interface IErrorLogService
	{
		Task<int> LogErrorAsync(
			string errorType,
			string errorTitle,
			string errorMessage,
			Exception? exception = null,
			object? additionalData = null,
			string? userId = null,
			string? userEmail = null,
			string? requestUrl = null,
			string? httpMethod = null,
			string? sessionId = null,
			int? statusCode = null);
	}

	public class ErrorLogService : IErrorLogService
	{
		private readonly DataContext _context;
		private readonly ILogger<ErrorLogService> _logger;
		private readonly IConfiguration _configuration;

		public ErrorLogService(DataContext context, ILogger<ErrorLogService> logger, IConfiguration configuration)
		{
			_context = context;
			_logger = logger;
			_configuration = configuration;
		}

		public async Task<int> LogErrorAsync(
			string errorType,
			string errorTitle,
			string errorMessage,
			Exception? exception = null,
			object? additionalData = null,
			string? userId = null,
			string? userEmail = null,
			string? requestUrl = null,
			string? httpMethod = null,
			string? sessionId = null,
			int? statusCode = null)
		{
			try
			{
				var errorLog = new ErrorLog
				{
					ErrorType = errorType,
					ErrorTitle = errorTitle,
					ErrorMessage = errorMessage,
					StackTrace = exception?.StackTrace,
					AdditionalData = additionalData != null 
						? JsonSerializer.Serialize(additionalData) 
						: null,
					UserId = userId,
					UserEmail = userEmail,
					RequestUrl = requestUrl,
					HttpMethod = httpMethod,
					SessionId = sessionId,
					StatusCode = statusCode,
					Environment = _configuration["Environment"] ?? "Unknown",
					CreatedAt = DateTime.UtcNow
				};

				_context.ErrorLogs.Add(errorLog);
				await _context.SaveChangesAsync();

				// Also log to server logs for immediate visibility
				_logger.LogError(exception, "Error logged: {ErrorType} - {ErrorTitle}. ID: {ErrorId}", 
					errorLog.ErrorType, errorLog.ErrorTitle, errorLog.Id);

				return errorLog.Id;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to log error to database");
				return -1;
			}
		}
	}
}
