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
