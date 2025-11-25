# Stale Claims Troubleshooting Guide

## Debug Page Available

A comprehensive debug page has been created at: **/debug/stale-claims**

This page allows you to:
- ? View current user info and token claims
- ? Toggle client assignment with one click
- ? Toggle Admin role on/off
- ? Change your name randomly
- ? See real-time logs of the token refresh process
- ? Monitor response headers for `X-Token-Refreshed`

## How to Use the Debug Page

### Step 1: Access the Page
1. Login as an Admin user
2. Navigate to **Debug Tools** ? **Stale Claims Test** in the left menu
3. Or go directly to: `https://localhost:7169/debug/stale-claims`

### Step 2: Verify Current State
The page will show:
- Your current email, name, client, and roles
- All claims in your JWT token
- Check that `UserModifiedAt` claim exists (?? important!)

### Step 3: Test Token Refresh
1. Click **"Toggle Client"** or **"Toggle Admin Role"**
2. Watch the debug logs - should see "User updated successfully"
3. Wait 1 second (automatic)
4. API call is triggered automatically
5. Watch for these log messages:
   - ? `X-Token-Refreshed header detected: true`
   - ? `New token received`
   - ? `User info retrieved` with updated values

### Step 4: Verify UI Updates
After the token refresh:
- Client ID/Name should update
- Roles should update
- Claims section should show new values
- **All without page refresh or logout!**

## Common Issues and Solutions

### Issue 1: "UserModifiedAt claim NOT FOUND in token"

**Problem**: Token doesn't have the timestamp claim needed for comparison.

**Solution**:
1. Check `AccountsController.GenerateJwtToken()` method
2. Verify this line exists:
   ```csharp
   new Claim("UserModifiedAt", user.LastModified.ToString("O"))
   ```
3. Logout and login again to get a new token with the claim

### Issue 2: "No token refresh detected - claims may already be current"

**Problem**: Server detected no changes, so it didn't refresh the token.

**Possible Causes**:
- User wasn't actually modified in database
- `LastModified` timestamp wasn't updated
- Token's `UserModifiedAt` is already newer than database

**Solution**:
1. Check server logs for "Stale claims detected" message
2. If not there, the middleware isn't detecting changes
3. Verify `UsersController.UpdateUser()` sets:
   ```csharp
   user.LastModified = DateTime.UtcNow;
   ```

### Issue 3: No "X-Token-Refreshed" header in response

**Problem**: Server might be generating token but not sending headers.

**Solution**:
1. Check `StaleClaimsMiddleware.cs` has these lines:
   ```csharp
   context.Response.Headers.Append("X-Token-Refresh", newToken);
   context.Response.Headers.Append("X-Token-Refreshed", "true");
   ```
2. Check Program.cs (API) CORS configuration exposes headers:
   ```csharp
   .WithExposedHeaders("X-Token-Refresh", "X-Token-Refreshed")
   ```

### Issue 4: Token received but not updating in client

**Problem**: JwtTokenHandler receives token but doesn't store it.

**Solution**:
1. Check `Program.cs` (Blazor) has `setToken` parameter:
   ```csharp
   new JwtTokenHandler(getToken, setToken, logger)
   ```
   NOT just:
   ```csharp
   new JwtTokenHandler(getToken, logger) // ? WRONG
   ```

### Issue 5: Token updates but UI doesn't refresh

**Problem**: CookieAuthenticationStateProvider not notifying Blazor.

**Solution**:
1. Check `CookieAuthenticationStateProvider` has monitoring task:
   ```csharp
   _ = MonitorTokenChangesAsync();
   ```
2. Verify `NotifyAuthenticationStateChanged()` is called
3. Check components use `AuthorizeView` or inject `AuthenticationState`

### Issue 6: Middleware not running at all

**Problem**: Middleware not in pipeline or in wrong order.

**Solution**:
1. Check `Program.cs` (API) has:
   ```csharp
   app.UseAuthentication();
   app.UseStaleClaimsDetection(); // Must be after Authentication
   app.UseAuthorization();
   ```
2. Must be AFTER `UseAuthentication()` (needs authenticated user)
3. Must be BEFORE `UseAuthorization()` (should run before auth checks)

## Debug Checklist

Use this checklist to diagnose issues:

### Server-Side Checks

- [ ] **Migration Applied**: `LastModified` column exists in database
  ```sql
  SELECT TOP 1 LastModified FROM AspNetUsers
  ```

- [ ] **Middleware Registered**: Check Program.cs has `app.UseStaleClaimsDetection()`

- [ ] **CORS Headers**: Check exposed headers include `X-Token-Refresh` and `X-Token-Refreshed`

- [ ] **Token Generation**: Login generates token with `UserModifiedAt` claim

- [ ] **LastModified Updates**: UsersController sets `user.LastModified = DateTime.UtcNow`

- [ ] **Middleware Logging**: Server logs show "Stale claims detected" when expected

### Client-Side Checks

- [ ] **JwtTokenHandler Setup**: Initialized with `getToken`, `setToken`, and `logger`

- [ ] **Token Storage**: `setToken` and `getToken` functions work correctly

- [ ] **Auth Provider**: CookieAuthenticationStateProvider monitoring token changes

- [ ] **Browser Console**: No CORS errors or JavaScript exceptions

- [ ] **Network Tab**: Response headers include `X-Token-Refreshed: true`

- [ ] **Components**: Using `AuthorizeView` or injecting `AuthenticationState`

## Logging Locations

### Server Logs (Visual Studio Output)
Look for these messages:
```
[Information] Stale claims detected for user {UserId} ({Email}). Token issued: {TokenTime:O}, User modified: {UserTime:O}. Refreshing token automatically.
[Information] New token generated for user {UserId} ({Email})
```

If NOT seeing these:
- Middleware isn't detecting stale claims
- Check timestamps in database vs token
- Verify middleware is in pipeline

### Client Logs (Browser Console)
Look for these messages:
```
[Information] Token refreshed automatically by server, updating local token
[Information] Token change detected, refreshing authentication state
```

If NOT seeing these:
- Handler might not have `setToken` callback
- Response headers might not be readable (CORS issue)
- Check browser console for errors

### Debug Page Logs
The debug page shows detailed logs including:
- All response headers (check for X-Token-Refresh*)
- Token updates and length
- User info changes
- Authentication state change events

## Testing Scenarios

### Scenario 1: Fresh Login
1. Logout completely
2. Login again
3. Check token claims - should have `UserModifiedAt`
4. Note the timestamp value
5. This is your baseline

### Scenario 2: Immediate Test
1. On debug page, click "Toggle Client"
2. Wait for "User updated successfully"
3. API call triggers automatically
4. Should see token refresh within 2 seconds
5. User info should update automatically

### Scenario 3: Multiple Changes
1. Toggle Client (should refresh)
2. Wait 2 seconds for state to settle
3. Toggle Admin Role (should refresh again)
4. Wait 2 seconds
5. Change Name (should refresh again)
6. Each should trigger a new token

### Scenario 4: No Change Test
1. On debug page, click "Trigger API Call"
2. Without modifying user first
3. Should see "No token refresh detected"
4. This is CORRECT - no changes = no refresh

## Performance Monitoring

### Expected Performance
- Token comparison: < 10ms (simple DateTime compare)
- Database query: < 50ms (single user lookup by ID)
- Token generation: < 100ms (JWT signing)
- Total overhead: < 200ms per API request (only when stale)

### Monitor These Metrics
- How often tokens are refreshed (should be rare)
- Database query frequency (only on API requests)
- Response time impact (should be minimal)

### Optimization Tips
If seeing performance issues:
1. Add database index on `LastModified` column
2. Consider caching user lookups (5-10 second TTL)
3. Only check claims on specific endpoints (not all `/api/*`)
4. Increase clock skew tolerance to reduce false positives

## API Endpoints for Manual Testing

### Get Current User Info
```bash
GET https://localhost:7237/api/accounts/info
Authorization: Bearer {your-token}
```

**Expected Response Headers** (if stale):
```
X-Token-Refreshed: true
X-Token-Refresh: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Update User (Force Stale)
```bash
PUT https://localhost:7237/api/users/{userId}
Authorization: Bearer {your-token}
Content-Type: application/json

{
  "email": "user@example.com",
  "givenName": "Test",
  "familyName": "User",
  "clientId": 1,
  "aribaId": null,
  "isDisabled": false,
  "roles": ["User", "Admin"]
}
```

### Verify LastModified Updated
Check database:
```sql
SELECT Id, Email, LastModified 
FROM AspNetUsers 
WHERE Email = 'user@example.com'
```

## Browser Developer Tools

### Network Tab Inspection
1. Open F12 Developer Tools
2. Go to Network tab
3. Make an API request
4. Click on the request
5. Look at Response Headers section
6. Search for `X-Token-Refresh`

**What to look for**:
- ? `X-Token-Refreshed: true` - Token was refreshed!
- ? `X-Token-Refresh: eyJ...` - New token present
- ? No X-Token headers - No refresh (check if needed)

### Console Tab Inspection
Look for:
- Errors in red (CORS, JavaScript exceptions)
- Warnings in yellow (Auth issues)
- Info logs from JwtTokenHandler
- Info logs from CookieAuthenticationStateProvider

## Database Queries for Debugging

### Check LastModified Values
```sql
SELECT 
    Id,
    Email,
    LastModified,
    ClientId,
    IsDisabled
FROM AspNetUsers
ORDER BY LastModified DESC
```

### Check Role Assignments
```sql
SELECT 
    u.Email,
    r.Name as RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'your-email@example.com'
```

### Force Update LastModified
```sql
UPDATE AspNetUsers
SET LastModified = GETUTCDATE()
WHERE Email = 'your-email@example.com'
```

## Quick Diagnostic Commands

### Test if middleware is registered
Check the logs after making any API request. Should see either:
- "Stale claims detected..." (if stale)
- OR nothing (if current)
- If you see authentication errors, middleware might not be running

### Test token generation
Decode your JWT token at https://jwt.io
Look for these claims:
- `sub` - User ID
- `email` - Email address
- `role` - User roles
- `UserModifiedAt` - ?? MUST EXIST
- `ClientId` - Client assignment
- `ClientName` - Client name

### Test CORS
Open browser console, run:
```javascript
fetch('https://localhost:7237/api/accounts/info', {
    headers: {
        'Authorization': 'Bearer ' + localStorage.getItem('jwt_token')
    }
})
.then(response => {
    console.log('Headers:', [...response.headers.entries()]);
    return response.json();
})
.then(data => console.log('Data:', data));
```

Should see X-Token-Refresh in headers if CORS is configured correctly.

## Support Resources

### Documentation Files
- `AutomaticTokenRefreshImplementation.md` - Full implementation details
- `AutomaticTokenRefreshQuickReference.md` - Quick reference
- `StaleClaimsFixSummary.md` - What was fixed and why

### Code Files to Review
- `StaleClaimsMiddleware.cs` - Server-side detection
- `JwtTokenHandler.cs` - Client-side token update
- `CookieAuthenticationStateProvider.cs` - State management
- `AccountsController.cs` - Token generation
- `UsersController.cs` - User updates
- `Program.cs` (both projects) - Configuration

### Debug Page
- `/debug/stale-claims` - Interactive testing tool
- Real-time logs
- One-click test buttons
- Current state display

## Still Not Working?

If you've gone through this entire guide and it's still not working:

1. **Check the debug page logs** - They show exactly what's happening
2. **Compare timestamps** - Token vs Database
3. **Verify middleware order** - Must be after UseAuthentication()
4. **Check CORS carefully** - Most common issue with headers
5. **Ensure setToken exists** - Handler must be able to update token
6. **Test with fresh login** - Old tokens might not have UserModifiedAt

### Contact Points
- Check GitHub issues for similar problems
- Review server logs in detail
- Use debug page to capture full sequence
- Compare your code with documentation examples

---

**Remember**: The debug page at `/debug/stale-claims` is your best friend for troubleshooting! It shows everything that's happening in real-time.
