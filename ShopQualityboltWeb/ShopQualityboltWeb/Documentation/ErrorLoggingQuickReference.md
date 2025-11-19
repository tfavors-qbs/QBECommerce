# Error Logging Quick Reference

## Basic Error Logging Template

### For New Controller Actions

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<MyResource>> CreateResource([FromBody] CreateResourceRequest request)
{
    try
    {
        // Your logic here
        var result = await _service.CreateAsync(request);
        
        _logger.LogInformation("Created resource {ResourceId} for user {Email}", result.Id, userEmail);
        
        return CreatedAtAction(nameof(GetResource), new { id = result.Id }, result);
    }
    catch (Exception ex)
    {
        await _errorLogService.LogErrorAsync(
            "API Error",                                                // Error type
            "Failed to Create Resource",                                // Error title
            ex.Message,                                                 // Error message
            ex,                                                         // Exception (optional)
            additionalData: new { /* contextual data */ },              // Additional data (optional)
            userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,   // User ID
            userEmail: User.FindFirst(ClaimTypes.Email)?.Value,         // User email
            requestUrl: HttpContext.Request.Path,                       // Request URL
            httpMethod: HttpContext.Request.Method);                    // HTTP method
        return StatusCode(500, new { message = "Failed to create resource" });
    }
}
```

## Constructor Injection

Add to your controller constructor:

```csharp
private readonly IErrorLogService _errorLogService;
private readonly ILogger<YourController> _logger;

public YourController(
    // ...other dependencies...
    IErrorLogService errorLogService,
    ILogger<YourController> logger)
{
    // ...other assignments...
    _errorLogService = errorLogService;
    _logger = logger;
}
```

## Common Scenarios

### 1. Not Found with Logging
```csharp
var resource = await _service.GetByIdAsync(id);
if (resource == null)
{
    // Optionally log if suspicious
    _logger.LogWarning("Resource {Id} not found", id);
    return NotFound();
}
```

### 2. Validation Error
```csharp
if (string.IsNullOrEmpty(request.RequiredField))
{
    await _errorLogService.LogErrorAsync(
        "Validation Error",
        "Missing Required Field",
        "RequiredField is null or empty",
        additionalData: new { requestData = request },
        userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method,
        statusCode: 400);
    return BadRequest(new { message = "RequiredField is required" });
}
```

### 3. Authorization Error
```csharp
if (resource.OwnerId != currentUserId)
{
    await _errorLogService.LogErrorAsync(
        "Authorization Error",
        "Unauthorized Resource Access",
        $"User {currentUserId} attempted to access resource {id} owned by {resource.OwnerId}",
        additionalData: new { resourceId = id, attemptedBy = currentUserId, owner = resource.OwnerId },
        userId: currentUserId,
        userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method,
        statusCode: 403);
    return Forbid();
}
```

### 4. Database Concurrency Error
```csharp
catch (DbUpdateConcurrencyException ex)
{
    if (!ResourceExists(id))
    {
        return NotFound();
    }
    
    await _errorLogService.LogErrorAsync(
        "Database Error",
        "Concurrency Conflict",
        ex.Message,
        ex,
        additionalData: new { resourceId = id },
        userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method);
    return Conflict(new { message = "Resource was modified by another user" });
}
```

### 5. External Service Error
```csharp
try
{
    var response = await _externalService.CallApiAsync(request);
    return Ok(response);
}
catch (HttpRequestException ex)
{
    await _errorLogService.LogErrorAsync(
        "External Service Error",
        "Failed to Call External API",
        ex.Message,
        ex,
        additionalData: new { 
            service = "ExternalServiceName", 
            endpoint = "api/endpoint",
            statusCode = ex.StatusCode 
        },
        userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method);
    return StatusCode(503, new { message = "External service unavailable" });
}
```

## Error Types Reference

Use these standardized error types:

| Error Type | Use Case |
|-----------|----------|
| `"PunchOut Setup Error"` | PunchOut session setup issues |
| `"PunchOut Checkout"` | Checkout process errors |
| `"Ariba Login Error"` | Ariba-specific auth issues |
| `"Login Error"` | General authentication failures |
| `"Account Registration Error"` | User registration problems |
| `"Shopping Cart Error"` | Cart operations |
| `"Contract Items Error"` | Contract item CRUD |
| `"User Management Error"` | User admin operations |
| `"Client Management Error"` | Client CRUD |
| `"Validation Error"` | Input validation failures |
| `"Authorization Error"` | Permission/access denied |
| `"Database Error"` | DB operation failures |
| `"External Service Error"` | Third-party API issues |
| `"API Error"` | Generic API errors |

## Tips

1. **Always await** - Error logging is async, always use `await`
2. **Log early** - Log before returning error response
3. **Context matters** - Include relevant IDs and state in `additionalData`
4. **Don't log sensitive data** - No passwords, tokens, or PII
5. **Use structured logging** - Pass objects to `additionalData`, not strings
6. **Consistent error types** - Use predefined types for better filtering
7. **Test error paths** - Ensure error logging works in catch blocks

## Viewing Logs

- **Admin UI**: Navigate to `/admin/error-logs` (Admin role required)
- **API**: `GET /api/errorlogs?errorType=YourType&isResolved=false`
- **Database**: Query `ErrorLogs` table directly

## Testing Error Logging

```csharp
// In your test
var errorLogServiceMock = new Mock<IErrorLogService>();
errorLogServiceMock
    .Setup(x => x.LogErrorAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<Exception>(),
        It.IsAny<object>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int?>()))
    .ReturnsAsync(1);

// Verify logging was called
errorLogServiceMock.Verify(x => x.LogErrorAsync(
    "Expected Error Type",
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<Exception>(),
    It.IsAny<object>(),
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<int?>()), 
    Times.Once);
```
