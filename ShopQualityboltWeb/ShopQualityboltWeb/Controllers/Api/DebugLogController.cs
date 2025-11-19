using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugLogController : ControllerBase
    {
        private readonly ILogger<DebugLogController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DebugLogController(ILogger<DebugLogController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [HttpPost("checkout-error")]
        [Authorize]
        public async Task<IActionResult> LogCheckoutError([FromBody] CheckoutErrorLog errorLog)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown";

                _logger.LogError(
                    "PunchOut Checkout Error - User: {UserEmail} ({UserId}), " +
                    "SessionId: {SessionId}, PostUrl: {PostUrl}, " +
                    "Error: {ErrorMessage}, StatusCode: {StatusCode}",
                    userEmail,
                    userId,
                    errorLog.SessionId,
                    errorLog.PostUrl,
                    errorLog.ErrorMessage,
                    errorLog.StatusCode
                );

                // Log debug details
                _logger.LogInformation(
                    "PunchOut Debug Details - {DebugSteps}",
                    string.Join(" | ", errorLog.DebugSteps)
                );

                if (!string.IsNullOrEmpty(errorLog.ResponseContent))
                {
                    _logger.LogInformation(
                        "Ariba Response Content: {ResponseContent}",
                        errorLog.ResponseContent.Substring(0, Math.Min(2000, errorLog.ResponseContent.Length))
                    );
                }

                // Optionally save to file in production for detailed analysis
                if (!_environment.IsDevelopment())
                {
                    await SaveToDebugFile(errorLog, userId, userEmail);
                }

                return Ok(new { message = "Error logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log checkout error");
                return StatusCode(500, "Failed to log error");
            }
        }

        private async Task SaveToDebugFile(CheckoutErrorLog errorLog, string userId, string userEmail)
        {
            try
            {
                var logsDir = Path.Combine(_environment.ContentRootPath, "Logs", "CheckoutErrors");
                Directory.CreateDirectory(logsDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"checkout_error_{timestamp}_{userId.Replace(":", "_")}.json";
                var filepath = Path.Combine(logsDir, filename);

                var logData = new
                {
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    UserEmail = userEmail,
                    ErrorLog = errorLog
                };

                var json = JsonSerializer.Serialize(logData, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filepath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save debug file");
            }
        }
    }

    public class CheckoutErrorLog
    {
        public string SessionId { get; set; } = "";
        public string PostUrl { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public int? StatusCode { get; set; }
        public List<string> DebugSteps { get; set; } = new();
        public string? ResponseContent { get; set; }
        public string? OrderMessage { get; set; }
        public int CartItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
