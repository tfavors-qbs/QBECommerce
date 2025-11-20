# Automatic Token Refresh Implementation

## Overview

Implemented an automatic JWT token refresh system that updates user claims seamlessly without requiring logout when user profiles are modified.

## Problem Solved

Previously, when a user's profile (roles, client assignment, name, etc.) was modified, the JWT token contained stale claims. The system would detect this and force the user to logout and login again, disrupting their workflow.

## Solution

The new implementation automatically generates a fresh token with updated claims and seamlessly updates it on the client side without interrupting the user's session.

## Components Modified

### 1. **StaleClaimsMiddleware.cs** (ShopQualityboltWeb)
- **Changed**: Now generates and returns a new JWT token instead of returning 401
- **How it works**:
  - Detects when token claims are stale by comparing `UserModifiedAt` timestamp
  - Generates new token with updated user information from database
  - Includes all current roles, client information, and user profile data
  - Returns token in response headers: `X-Token-Refresh` and `X-Token-Refreshed: true`
  - Continues processing the original request (doesn't interrupt it)

### 2. **JwtTokenHandler.cs** (QBExternalWebLibrary)
- **Changed**: Now intercepts response headers and automatically updates stored token
- **How it works**:
  - Adds JWT token to all outgoing requests (existing functionality)
  - Checks response headers for `X-Token-Refreshed` flag
  - If refreshed token is present, updates the local token storage
  - All future requests automatically use the new token

### 3. **CookieAuthenticationStateProvider.cs** (QBExternalWebLibrary)
- **Changed**: Added token monitoring to detect changes and refresh authentication state
- **How it works**:
  - Background task monitors token changes every second
  - When token changes (e.g., from middleware refresh), triggers authentication state update
  - Blazor components automatically re-render with new claims
  - Components using `AuthorizeView`, `[Authorize]`, or `AuthenticationState` see changes immediately

### 4. **Program.cs** (ShopQualityboltWeb)
- **Changed**: Added new headers to CORS exposed headers
- **Added**: `X-Token-Refresh` and `X-Token-Refreshed` to both CORS policies
- **Why**: Allows client-side code to read custom response headers

### 5. **Program.cs** (ShopQualityboltWebBlazor)
- **Changed**: Updated JwtTokenHandler initialization to pass `setToken` callback
- **Why**: Allows handler to update the token when middleware refreshes it

## Flow Diagram

```
1. User makes API request with JWT token
2. Middleware checks if user profile has been modified since token was issued
3. IF MODIFIED:
   a. Middleware generates new token with current user data
   b. Middleware adds token to response headers
   c. Middleware continues processing request normally
   d. Response returns to client WITH the new token in headers
4. JwtTokenHandler intercepts response
5. JwtTokenHandler detects refresh headers
6. JwtTokenHandler updates stored token
7. CookieAuthenticationStateProvider detects token change
8. AuthenticationStateProvider notifies Blazor of state change
9. Blazor components re-render with new claims
```

## User Experience

### Before (Old Behavior)
1. Admin edits user's role or client assignment
2. User makes next API request
3. Server returns 401 Unauthorized
4. User sees error message: "Your session is out of date. Please log out and log back in."
5. User must logout
6. User must login again
7. User loses current page state/work

### After (New Behavior)
1. Admin edits user's role or client assignment
2. User makes next API request
3. Server detects stale claims and generates new token
4. Request completes successfully
5. Client automatically updates token in background
6. Blazor components seamlessly update to reflect new permissions
7. **User continues working without interruption**

## Technical Details

### Token Generation
- Uses same `GenerateJwtTokenAsync` logic as login
- Includes all current roles from UserManager
- Includes updated client information
- Includes current `UserModifiedAt` timestamp
- Same expiration time as original token

### Security Considerations
- Only generates new token if user is already authenticated
- Only works for API requests (starts with `/api`)
- Maintains same security validation as login
- User must still have valid, non-expired token to trigger refresh
- Refresh happens server-side only (client cannot request it)

### Performance Impact
- Minimal: Only checks on API requests from authenticated users
- Database query only when token exists and has `UserModifiedAt` claim
- Token generation only when modification detected (rare occurrence)
- No impact on requests with current tokens

## Configuration

No configuration changes required. The system uses existing JWT settings:
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpireMinutes`

## Testing Scenarios

### Test 1: Role Change
1. Login as a user with "User" role
2. Have admin change role to "Admin"
3. Make any API request
4. Verify new token is received
5. Verify `AuthorizeView` components update to show admin options

### Test 2: Client Assignment
1. Login as a user assigned to Client A
2. Have admin change to Client B
3. Make any API request
4. Verify new token contains Client B information
5. Verify UI shows Client B data

### Test 3: Multiple Changes
1. Login as user
2. Have admin change name, role, and client
3. Make API request
4. Verify all changes reflected in new token
5. Verify UI updates with all new information

### Test 4: No Changes
1. Login as user
2. Make multiple API requests
3. Verify no token refresh occurs (performance test)
4. Verify normal operations continue

## Logging

The system logs the following events:
- **Information**: When stale claims detected and new token generated
- **Information**: When client successfully updates token
- **Error**: Any errors during claim validation or token generation

Example log entries:
```
[Information] Stale claims detected for user abc123 (user@example.com). Token issued: 2024-01-15T10:00:00, User modified: 2024-01-15T10:05:00. Refreshing token automatically.
[Information] New token generated for user abc123 (user@example.com)
[Information] Token refreshed automatically by server, updating local token
[Information] Token change detected, refreshing authentication state
```

## Future Enhancements

### Potential Improvements
1. **SignalR Integration**: Push token updates proactively instead of waiting for next API call
2. **Token Expiration Refresh**: Automatically refresh tokens before they expire
3. **Refresh Token Pattern**: Implement proper refresh token flow for long-lived sessions
4. **Token Revocation**: Add ability to force token refresh/invalidation for security events
5. **Metrics**: Track how often tokens are refreshed for monitoring

### Not Included (Out of Scope)
- Refresh token storage in database
- Token blacklisting/revocation
- Automatic refresh before expiration
- Push notifications for token updates

## Troubleshooting

### Token Not Updating
- Check browser console for CORS errors
- Verify `X-Token-Refresh` header is in CORS exposed headers
- Confirm JwtTokenHandler is registered with both getToken and setToken callbacks
- Check middleware order in Program.cs (must be after Authentication)

### Components Not Updating
- Verify CookieAuthenticationStateProvider is monitoring tokens
- Check that AuthenticationStateChanged is being triggered
- Ensure components use `AuthorizeView` or `AuthenticationState` dependency

### Performance Issues
- Check logs for excessive token refreshes
- Verify `UserModifiedAt` is only updated when necessary
- Consider adding caching if database queries are too frequent

## Related Files

- `ShopQualityboltWeb/Middleware/StaleClaimsMiddleware.cs`
- `QBExternalWebLibrary/Services/Authentication/JwtTokenHandler.cs`
- `QBExternalWebLibrary/Services/Authentication/CookieAuthenticationStateProvider.cs`
- `ShopQualityboltWeb/Program.cs`
- `ShopQualityboltWebBlazor/Program.cs`
- `ShopQualityboltWeb/Controllers/Api/AccountsController.cs`
- `ShopQualityboltWeb/Controllers/Api/UsersController.cs`

## Conclusion

The automatic token refresh system provides a seamless user experience when user profiles are modified, eliminating the need for forced logouts while maintaining security and data accuracy. The implementation is efficient, transparent to users, and integrates naturally with the existing JWT authentication system.
