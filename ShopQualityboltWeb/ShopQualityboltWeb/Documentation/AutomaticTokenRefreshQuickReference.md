# Automatic Token Refresh - Quick Reference

## What Changed

When a user's profile is edited (role, client, name, etc.), their JWT token is automatically refreshed with new claims **without requiring logout**.

## How It Works

```
User Request ? Middleware Detects Stale Claims ? Generates New Token ? 
Returns in Header ? Client Updates Token ? Blazor Re-renders
```

## Key Files Modified

| File | Change |
|------|--------|
| `StaleClaimsMiddleware.cs` | Generates new token instead of returning 401 |
| `JwtTokenHandler.cs` | Auto-updates token from response headers |
| `CookieAuthenticationStateProvider.cs` | Monitors token changes, triggers UI updates |
| `Program.cs` (API) | Exposes token refresh headers in CORS |
| `Program.cs` (Blazor) | Passes setToken to handler |

## Response Headers

When middleware detects stale claims:
```
X-Token-Refreshed: true
X-Token-Refresh: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## User Experience

### Before ?
```
1. Admin changes user role
2. User gets 401 error
3. User must logout
4. User must login again
5. User loses work in progress
```

### After ?
```
1. Admin changes user role
2. Next API call auto-refreshes token
3. UI updates automatically
4. User continues working
5. No interruption!
```

## Testing

### Quick Test
1. Login as a user
2. Have admin change your role/client
3. Click anywhere that makes an API call
4. ? Should see updated permissions immediately
5. ? No logout required

### Check Logs
```
[Information] Stale claims detected for user {UserId}. Refreshing token automatically.
[Information] Token refreshed automatically by server, updating local token
[Information] Token change detected, refreshing authentication state
```

## Troubleshooting

### Problem: Token not updating
**Check**: Browser console for CORS errors  
**Fix**: Verify `X-Token-Refresh` in exposed headers (Program.cs)

### Problem: UI not updating
**Check**: Token is being updated but components don't refresh  
**Fix**: Ensure components use `AuthorizeView` or inject `AuthenticationState`

### Problem: Too many refreshes
**Check**: Logs for excessive token generation  
**Fix**: Ensure `LastModified` only updates when necessary

## Code Examples

### In Controllers (Auto-triggers refresh)
```csharp
// When updating user
user.LastModified = DateTime.UtcNow;  // ? This triggers refresh
await _userManager.UpdateAsync(user);
```

### In Blazor Components (Auto-receives update)
```razor
<AuthorizeView Roles="Admin">
    <!-- This will update automatically when role changes -->
    <Authorized>
        <MudButton>Admin Only</MudButton>
    </Authorized>
</AuthorizeView>
```

## Configuration

Uses existing JWT settings:
```json
{
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "ExpireMinutes": 30
  }
}
```

## Performance

- ? Minimal impact - only checks authenticated API requests
- ? Only queries DB when token has UserModifiedAt claim
- ? Only generates token when modification detected
- ? No impact on requests with current tokens

## Security

- ? Requires valid, authenticated token to trigger
- ? Server-side only (client cannot force refresh)
- ? Uses same security as login
- ? Maintains token expiration
- ? Only works on API endpoints

## When Token Refreshes

- ? User role added/removed
- ? User client assignment changed
- ? User name changed (GivenName/FamilyName)
- ? User disabled/enabled status changed
- ? Password change (no LastModified update)
- ? Email change (requires email confirmation)

## Important Notes

1. **Middleware Order**: Must be after `UseAuthentication()` in Program.cs
2. **CORS Headers**: Must expose `X-Token-Refresh` and `X-Token-Refreshed`
3. **Token Storage**: setToken callback must be passed to JwtTokenHandler
4. **UI Updates**: Components using AuthenticationState will auto-update

## Support

For issues or questions, check:
1. `AutomaticTokenRefreshImplementation.md` (full documentation)
2. Application logs (look for "Token refresh" or "Stale claims")
3. Browser console (network tab, check response headers)
4. Database (verify LastModified is updating)

## Related Documentation

- `ErrorLoggingImplementation.md`
- `ErrorLoggingQuickReference.md`
- JWT Authentication setup docs

---

**Status**: ? Implemented and Tested  
**Version**: 1.0  
**Date**: 2024  
**Build**: ? All projects compile successfully
