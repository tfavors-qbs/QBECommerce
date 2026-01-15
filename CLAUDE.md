# Project Guidelines

## Error Logging
When creating or modifying API controllers, always add error logging using the `IErrorLogService`:

1. Inject `IErrorLogService` in the constructor
2. Add `using ShopQualityboltWeb.Services;` and `using System.Security.Claims;`
3. Wrap all endpoint methods in try-catch blocks
4. Log errors with:
   - Error type (e.g., "Material Error")
   - Error title (e.g., "Failed to Get Material")
   - Exception message and object
   - User context (userId, userEmail from Claims)
   - Request context (requestUrl, httpMethod)
   - Entity-specific additionalData when applicable

Example pattern:
```csharp
private readonly IErrorLogService _errorLogService;

public MyController(IMyService service, IErrorLogService errorLogService) {
    _service = service;
    _errorLogService = errorLogService;
}

[HttpGet("{id}")]
public async Task<ActionResult<Entity>> GetEntity(int id) {
    try {
        var entity = _service.GetById(id);
        if (entity == null) {
            return NotFound();
        }
        return entity;
    } catch (Exception ex) {
        await _errorLogService.LogErrorAsync(
            "Entity Error",
            "Failed to Get Entity",
            ex.Message,
            ex,
            additionalData: new { entityId = id },
            userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
            requestUrl: HttpContext.Request.Path,
            httpMethod: HttpContext.Request.Method);
        return StatusCode(500, new { message = "Failed to retrieve entity" });
    }
}
```

## UI Error Logging (Blazor Pages & Components)

When creating or modifying Blazor pages and components, add error logging for failed operations by posting to `api/errorlogs`. This ensures UI-level failures are tracked alongside API errors.

### When to Log
- API calls that return null or fail
- Operations that catch exceptions
- Any significant user-facing failure

### Implementation Pattern

1. Inject `IHttpClientFactory` and create an "Auth" client
2. When an operation fails, post error details to `api/errorlogs`
3. Show user-friendly Snackbar message (don't expose technical details)
4. Wrap logging in try-catch to prevent logging failures from breaking UI

Example pattern:
```razor
@inject IHttpClientFactory HttpClientFactory
@inject ISnackbar Snackbar

@code {
    private async Task LogErrorAsync(string errorType, string title, string message, object? additionalData = null)
    {
        try
        {
            var httpClient = HttpClientFactory.CreateClient("Auth");
            var errorLog = new
            {
                ErrorType = errorType,
                ErrorTitle = title,
                ErrorMessage = message,
                AdditionalData = additionalData
            };
            await httpClient.PostAsJsonAsync("api/errorlogs", errorLog);
        }
        catch
        {
            // Silently fail - logging errors shouldn't break the UI
        }
    }

    private async Task SomeOperation()
    {
        var result = await SomeApiService.DoSomethingAsync();
        if (result == null)
        {
            await LogErrorAsync("UI Error", "Operation Failed", "DoSomething returned null", new { context = "relevant data" });
            Snackbar.Add("Operation failed", Severity.Error);
            return;
        }
        // Success path...
    }
}
```
