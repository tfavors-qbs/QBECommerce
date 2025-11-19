# JWT Token-Based Authentication - Unified Implementation

## Overview

**All authentication now uses JWT tokens** for both regular login and Ariba PunchOut. This provides:
- ? **Consistent authentication** across all login methods
- ? **Better security** (stateless, no session storage)
- ? **Cross-domain compatibility** (works in iframes)
- ? **Modern best practice** (standard OAuth 2.0 Bearer tokens)

## Authentication Flow

### Both Regular Login AND Ariba PunchOut Now Use JWT Tokens

```
User Login (any method)
    ?
API validates credentials
    ?
API generates JWT token with claims (user ID, email, roles, etc.)
    ?
API returns: { "accessToken": "eyJhbGci...", "tokenType": "Bearer", "expiresIn": 3600 }
    ?
Blazor stores token in memory (singleton closure)
    ?
JwtTokenHandler automatically adds "Authorization: Bearer {token}" to ALL requests
    ?
All subsequent API calls authenticated via token
    ?
Token expires after 60 minutes (configurable)
```

## Implementation Details

### 1. Regular Login (Email/Password)

**Endpoint**: `POST /api/accounts/login`

**Request**:
```json
{
  "email": "user@example.com",
  "password": "MyPassword123!"
}
```

**Response**:
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6"
}
```

### 2. Ariba PunchOut Login

**Endpoint**: `POST /api/accounts/login/ariba`

**Request**:
```json
"12345678"  // PunchOut session ID
```

**Response**:
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "12345678"  // Session ID used as refresh token
}
```

## Token Contents (JWT Claims)

When decoded, the JWT token contains:

```json
{
  "sub": "user-id-guid",
  "email": "user@example.com",
  "given_name": "John",
  "family_name": "Doe",
  "nameid": "user-id-guid",
  "name": "user@example.com",
  "ClientId": "123",
  "role": ["User", "Admin"],  // All user roles
  "jti": "unique-token-id",
  "nbf": 1234567890,
  "exp": 1234571490,  // Expiration timestamp
  "iat": 1234567890,
  "iss": "ShopQualityboltWeb",
  "aud": "ShopQualityboltWebBlazor"
}
```

## How It Works

### Component Architecture

```
???????????????????????????????????????????????????
?  Blazor Server App                              ?
?  ?????????????????????????????????????????????  ?
?  ?  CookieAuthenticationStateProvider       ?  ?
?  ?  - Stores token via _setToken()          ?  ?
?  ?  - Gets token via _getToken()            ?  ?
?  ?????????????????????????????????????????????  ?
?               ?                                  ?
?               ?                                  ?
?  ?????????????????????????????????????????????  ?
?  ?  Singleton Token Storage (Program.cs)    ?  ?
?  ?  string? jwtToken = null;                ?  ?
?  ?  Func<string?> getToken = () => jwtToken;?  ?
?  ?  Action<string?> setToken = (t) => ...;  ?  ?
?  ?????????????????????????????????????????????  ?
?               ?                                  ?
?               ?                                  ?
?  ?????????????????????????????????????????????  ?
?  ?  JwtTokenHandler (DelegatingHandler)     ?  ?
?  ?  - Intercepts ALL HTTP requests          ?  ?
?  ?  - Adds Authorization: Bearer {token}    ?  ?
?  ?????????????????????????????????????????????  ?
?               ?                                  ?
????????????????????????????????????????????????????
                ?
                ? HTTP Request with Authorization header
?????????????????????????????????????????????????????
?  Web API                                          ?
?  ???????????????????????????????????????????????  ?
?  ?  JWT Bearer Authentication Middleware      ?  ?
?  ?  - Validates token signature               ?  ?
?  ?  - Validates issuer & audience             ?  ?
?  ?  - Validates expiration                    ?  ?
?  ?  - Extracts claims ? User.Identity         ?  ?
?  ???????????????????????????????????????????????  ?
?               ?                                  ?
?               ?                                  ?
?  ???????????????????????????????????????????????  ?
?  ?  [Authorize] Controllers                   ?  ?
?  ?  - User.Identity.IsAuthenticated = true   ?  ?
?  ?  - User claims available for authorization ?  ?
?  ???????????????????????????????????????????????  ?
?????????????????????????????????????????????????????
```

## Benefits Over Cookie-Based Authentication

| Feature | Cookie Auth | JWT Token Auth |
|---------|-------------|----------------|
| **Cross-domain** | ? Blocked by browser | ? Works via headers |
| **Iframe support** | ? Third-party cookie restrictions | ? No restrictions |
| **Stateless** | ? Requires server-side session storage | ? No server state needed |
| **Scalability** | ? Session affinity required | ? Any server can validate |
| **Mobile/API** | ? Cookies don't work well | ? Perfect for APIs |
| **CORS** | ?? Complex configuration | ? Simple header-based |
| **Security** | ?? CSRF vulnerable | ? CSRF immune |
| **Token size** | ~4KB (cookie limit) | ~1-2KB (typical JWT) |
| **Inspection** | ? Opaque | ? Can decode and inspect |

## Security Features

### 1. Token Validation
```csharp
// In API Program.cs
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,           // Must be from our API
    ValidateAudience = true,         // Must be for our Blazor app
    ValidateLifetime = true,         // Must not be expired
    ValidateIssuerSigningKey = true, // Must have valid signature
    ClockSkew = TimeSpan.Zero        // No grace period
};
```

### 2. Token Expiration
- **Default**: 60 minutes (configurable via `Jwt:ExpireMinutes`)
- **Automatic**: Token becomes invalid after expiration
- **Refresh**: Use refresh token to get new access token (future enhancement)

### 3. Secure Storage
- **Memory only**: Token stored in server-side memory (not localStorage)
- **Not exposed**: Token never accessible to client-side JavaScript
- **HTTPS only**: Token only transmitted over secure connections

### 4. Role-Based Authorization
```csharp
// Roles included in token claims
claims.Add(new Claim(ClaimTypes.Role, "User"));
claims.Add(new Claim(ClaimTypes.Role, "Admin"));

// Controllers can enforce roles
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase { }
```

## Configuration

### Required Settings (appsettings.json)

```json
{
  "Jwt": {
    "Key": "YourSecretKeyMustBeAtLeast32CharactersLongForSecurity!",
    "Issuer": "ShopQualityboltWeb",
    "Audience": "ShopQualityboltWebBlazor",
    "ExpireMinutes": "60"
  },
  "ApiSettings": {
    "BaseAddress": "https://localhost:7237"
  },
  "BackendUrl": "https://localhost:7237",
  "FrontendUrl": "https://localhost:7169"
}
```

### Production Considerations

1. **Secret Key Management**
   - Store `Jwt:Key` in Azure Key Vault
   - Never commit secrets to source control
   - Rotate keys periodically

2. **HTTPS Required**
   - JWT tokens MUST be transmitted over HTTPS
   - Configure HSTS headers
   - Enable certificate validation

3. **Token Expiration**
   - Balance security vs. user experience
   - Shorter tokens = more secure but more frequent re-login
   - Consider implementing refresh token flow

## Debug Output

When running in debug mode, you'll see console output like:

```
[JWT AUTH] Starting regular login for email: user@example.com
[JWT AUTH] Regular login response status: OK
[JWT AUTH] Response: {"tokenType":"Bearer","accessToken":"eyJhbGci...
[JWT AUTH] Token stored successfully for user@example.com. Length: 542
[JwtTokenHandler] Added Authorization header to GET /api/accounts/info
[JwtTokenHandler] Token: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI...
[JWT AUTH] api/accounts/info response: OK
[JWT AUTH] User authenticated successfully: user@example.com
```

## Troubleshooting

### Token Not Being Sent

**Symptom**: API returns 401 Unauthorized
**Check**:
1. Token is stored: `_getToken()` returns non-null
2. JwtTokenHandler is registered in DI
3. HttpClient is using "Auth" client name
4. Check browser DevTools ? Network ? Request Headers

### Token Validation Fails

**Symptom**: API logs "Token validation failed"
**Check**:
1. `Jwt:Key` matches between Blazor and API
2. `Jwt:Issuer` matches
3. `Jwt:Audience` matches
4. Token hasn't expired
5. Decode token at jwt.io to inspect claims

### Role Authorization Fails

**Symptom**: User authenticated but gets 403 Forbidden
**Check**:
1. Roles are included in token (decode at jwt.io)
2. User actually has the required role in database
3. Role claim type matches (`ClaimTypes.Role`)
4. `[Authorize(Roles = "...")]` attribute is correct

## Migration Notes

### If Migrating from Cookie-Based Auth

1. **Existing Sessions**: Users will need to re-login once
2. **Cookies**: Old auth cookies will be ignored (harmless)
3. **Logout**: Clears both token and any residual cookies
4. **No Breaking Changes**: All existing endpoints work the same

### Testing Checklist

- [ ] Regular login works (email/password)
- [ ] Ariba PunchOut login works (sessionId)
- [ ] `/api/accounts/info` returns 200 OK
- [ ] User roles are enforced correctly
- [ ] Token expiration works (wait 60 minutes)
- [ ] Logout clears token
- [ ] Re-login after logout works
- [ ] Multiple tabs/windows work correctly
- [ ] Browser refresh maintains authentication

## Future Enhancements

1. **Refresh Token Flow**
   - Implement automatic token refresh
   - Store refresh token securely
   - Seamless re-authentication

2. **Token Revocation**
   - Maintain blacklist of revoked tokens
   - Immediate logout capability
   - Admin can revoke user tokens

3. **Remember Me**
   - Longer-lived refresh tokens
   - Persist token across browser restarts
   - Optional feature with user consent

4. **Multi-Factor Authentication**
   - Add MFA before issuing token
   - Include MFA claims in token
   - Require MFA for sensitive operations

---

**Status**: ? **FULLY IMPLEMENTED**  
**Applies To**: All authentication (Regular + Ariba PunchOut)  
**Breaking Changes**: None - Users just need to re-login once  
**Security Level**: ?? **HIGH** - Modern OAuth 2.0 standard
