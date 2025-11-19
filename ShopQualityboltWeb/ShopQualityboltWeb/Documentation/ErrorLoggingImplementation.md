# Error Logging Implementation

## Overview

Comprehensive error logging has been implemented throughout the API using a centralized `ErrorLogService`. This system captures and stores errors in the database for monitoring, debugging, and analysis.

## Components

### 1. ErrorLogService (`Services/ErrorLogService.cs`)

A centralized service that handles error logging to the database.

**Features:**
- Async error logging to database
- Captures exception details including stack traces
- Stores contextual information (user, request, session)
- Supports additional metadata as JSON
- Falls back to ILogger if database logging fails

**Usage:**
```csharp
await _errorLogService.LogErrorAsync(
    errorType: "API Error",
    errorTitle: "Failed to Process Request",
    errorMessage: ex.Message,
    exception: ex,
    additionalData: new { userId = 123, action = "checkout" },
    userId: currentUserId,
    userEmail: currentUserEmail,
    requestUrl: HttpContext.Request.Path,
    httpMethod: HttpContext.Request.Method,
    sessionId: sessionId,
    statusCode: 500
);
```

### 2. Updated Controllers

The following controllers have been updated with comprehensive error logging:

#### Core Controllers (Updated):
1. **PunchOutSessionsController** - PunchOut session management and setup
2. **AccountsController** - Authentication and user registration
3. **ShoppingCartsAPIController** - Shopping cart operations
4. **ContractItemsApiController** - Contract item management
5. **UsersController** - User management (admin)
6. **ClientsApiController** - Client management
7. **ErrorLogsController** - Error log viewing and management (meta-logging)

### 3. Error Types

Errors are categorized by type for easy filtering:

- **"PunchOut Setup Error"** - Issues during PunchOut session setup
- **"PunchOut Checkout"** - Errors during checkout process
- **"Ariba Login Error"** - Authentication issues for Ariba users
- **"Login Error"** - General authentication failures
- **"Account Registration Error"** - User registration problems
- **"Account Info Error"** - Issues retrieving user info
- **"Shopping Cart Error"** - Cart operation failures
- **"Contract Items Error"** - Contract item CRUD errors
- **"User Management Error"** - User administration errors
- **"Client Management Error"** - Client CRUD errors
- **"API Error"** - General API errors
- **"Error Log System Error"** - Meta-errors in the logging system itself

## Error Logging Patterns

### Pattern 1: Logging Validation Errors
```csharp
if (requiredData == null)
{
    await _errorLogService.LogErrorAsync(
        "Validation Error",
        "Missing Required Data",
        "Required field was null or empty",
        additionalData: new { field = "requiredData" },
        userId: userId,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method,
        statusCode: 400);
    return BadRequest("Required data missing");
}
```

### Pattern 2: Logging Exceptions
```csharp
catch (Exception ex)
{
    await _errorLogService.LogErrorAsync(
        "Operation Error",
        "Failed to Complete Operation",
        ex.Message,
        ex,
        additionalData: new { resourceId = id },
        userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method);
    return StatusCode(500, new { message = "Operation failed" });
}
```

### Pattern 3: Logging Authorization Failures
```csharp
if (resource.OwnerId != currentUserId)
{
    await _errorLogService.LogErrorAsync(
        "Authorization Error",
        "Unauthorized Access Attempt",
        $"User attempted to access resource {id}",
        additionalData: new { resourceId = id, resourceOwnerId = resource.OwnerId },
        userId: currentUserId,
        userEmail: currentUserEmail,
        requestUrl: HttpContext.Request.Path,
        httpMethod: HttpContext.Request.Method,
        statusCode: 403);
    return Forbid();
}
```

## Database Schema

Error logs are stored in the `ErrorLogs` table with the following key fields:

- **Id** - Auto-incrementing primary key
- **ErrorType** - Category of error (indexed for filtering)
- **ErrorTitle** - Short description
- **ErrorMessage** - Detailed error message
- **StackTrace** - Exception stack trace (if available)
- **AdditionalData** - JSON string with contextual data
- **UserId** - ID of user who encountered the error
- **UserEmail** - Email of user (for quick reference)
- **RequestUrl** - API endpoint where error occurred
- **HttpMethod** - HTTP method (GET, POST, etc.)
- **IpAddress** - Client IP address
- **UserAgent** - Client browser/app info
- **SessionId** - PunchOut session ID (if applicable)
- **StatusCode** - HTTP status code
- **Environment** - Deployment environment
- **CreatedAt** - Timestamp
- **IsResolved** - Whether error has been addressed
- **ResolvedAt** - Resolution timestamp
- **ResolvedBy** - Admin who resolved it
- **ResolutionNotes** - Resolution details

## Admin Interface

Admins can view and manage error logs through:

1. **API Endpoints:**
   - `GET /api/errorlogs` - List all errors (with filters)
   - `GET /api/errorlogs/{id}` - Get specific error
   - `GET /api/errorlogs/stats` - Get error statistics
   - `PUT /api/errorlogs/{id}/resolve` - Mark error as resolved
   - `DELETE /api/errorlogs/{id}` - Delete error log

2. **Blazor Admin Page:**
   - `AdminErrorLogs.razor` - Full error log viewer with filtering

## Best Practices

### 1. Always Log Context
Include relevant IDs, user information, and request details:
```csharp
additionalData: new { 
    cartId = cart.Id, 
    itemCount = items.Count,
    totalValue = total 
}
```

### 2. Don't Expose Sensitive Data in Error Messages
- Don't log passwords, tokens, or credit card numbers
- Sanitize error messages returned to clients
- Full details go to database, generic message to client

### 3. Log Before Returning Errors
Always log before returning error responses so the error is captured even if the client disconnects.

### 4. Use Appropriate Error Types
Use consistent error type names for filtering and reporting.

### 5. Include User Context
Always include userId and userEmail when available (from User.Claims).

## Performance Considerations

- Error logging is asynchronous to minimize impact on response time
- Database writes use efficient single-insert operations
- Failed error logs fall back to ILogger (won't crash the app)
- Consider archiving old resolved errors periodically

## Monitoring and Alerts

To set up monitoring:

1. Query unresolved errors regularly
2. Set up alerts for critical error types
3. Review error statistics dashboard weekly
4. Track error trends over time

**Example Query for Critical Errors:**
```sql
SELECT * FROM ErrorLogs 
WHERE IsResolved = 0 
  AND ErrorType IN ('PunchOut Setup Error', 'Ariba Login Error')
  AND CreatedAt >= DATEADD(day, -1, GETDATE())
ORDER BY CreatedAt DESC
```

## Future Enhancements

Consider adding:
- Email alerts for critical errors
- Integration with external monitoring tools (Application Insights, Sentry)
- Error rate limiting to prevent log flooding
- Automatic error categorization using ML
- Error deduplication (group similar errors)

## Support

For issues or questions about error logging:
1. Check the ErrorLogService implementation
2. Review existing error logs for patterns
3. Ensure ErrorLogService is registered in DI (Program.cs)
4. Verify database connection and ErrorLogs table exists
