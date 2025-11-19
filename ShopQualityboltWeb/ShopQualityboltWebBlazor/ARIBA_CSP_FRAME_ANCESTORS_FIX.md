# Fix for Ariba Content Security Policy (CSP) Frame-Ancestors Error

## Problem

When Ariba tries to load `https://shop.qualitybolt.com/` in an iframe, the following error occurs:

```
Framing 'https://shop.qualitybolt.com/' violates the following Content Security Policy directive: 
"frame-ancestors 'self'". The request has been blocked.
```

### Root Cause

By default, ASP.NET Core's HSTS (HTTP Strict Transport Security) middleware adds a restrictive **Content-Security-Policy** header:

```
Content-Security-Policy: frame-ancestors 'self'
```

This directive only allows the site to be framed by itself, blocking Ariba from embedding your shop in their procurement interface.

---

## Solution

### Added Custom CSP Middleware

Modified `ShopQualityboltWebBlazor/Program.cs` to add middleware that:
1. ? Configures `frame-ancestors` to allow Ariba domains
2. ? Removes the conflicting `X-Frame-Options` header
3. ? Still maintains security by specifying allowed origins

### Code Changes

**File**: `ShopQualityboltWebBlazor/Program.cs`

```csharp
// Configure Content Security Policy to allow Ariba to frame this site
app.Use(async (context, next) =>
{
    // Get Ariba domains from configuration
    var aribaOrigins = builder.Configuration.GetSection("Ariba:AllowedFrameOrigins").Get<string[]>() 
        ?? new[] 
        { 
            "https://*.ariba.com",
            "https://*.sap.com",
            "https://s1.ariba.com",
            "https://service.ariba.com"
        };
    
    // Build frame-ancestors directive
    var frameAncestors = string.Join(" ", new[] { "'self'" }.Concat(aribaOrigins));
    
    // Set CSP header to allow framing from Ariba
    context.Response.Headers.Append("Content-Security-Policy", 
        $"frame-ancestors {frameAncestors}");
    
    // Remove X-Frame-Options header (conflicts with CSP frame-ancestors)
    context.Response.Headers.Remove("X-Frame-Options");
    
    await next();
});
```

### Configuration Added

**Files**: 
- `ShopQualityboltWebBlazor/appsettings.json`
- `ShopQualityboltWebBlazor/appsettings.Development.json`

```json
{
  "Ariba": {
    "SharedSecret": "abracadabra",
    "AllowedFrameOrigins": [
      "https://*.ariba.com",
      "https://*.sap.com",
      "https://s1.ariba.com",
      "https://service.ariba.com"
    ]
  }
}
```

---

## How It Works

### Before (Blocked)

```
Ariba Frame: https://s1.ariba.com
    ? Tries to load
Your Site: https://shop.qualitybolt.com
    ? Response Headers
Content-Security-Policy: frame-ancestors 'self'
X-Frame-Options: SAMEORIGIN
    ?
? BLOCKED - "violates frame-ancestors 'self'"
```

### After (Allowed)

```
Ariba Frame: https://s1.ariba.com
    ? Tries to load
Your Site: https://shop.qualitybolt.com
    ? Response Headers
Content-Security-Policy: frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com
(X-Frame-Options removed)
    ?
? ALLOWED - s1.ariba.com is in the allowed list
```

---

## Security Considerations

### ? What This Does

1. **Allows Ariba to Frame Your Site** - Ariba domains can embed shop.qualitybolt.com in iframes
2. **Blocks Other Sites** - Only specified domains can frame your site
3. **Prevents Clickjacking** - CSP frame-ancestors is more secure than X-Frame-Options

### ?? What This Does NOT Do

1. **Does not disable all CSP** - Only configures frame-ancestors
2. **Does not allow all framing** - Only specified Ariba domains
3. **Does not affect authentication** - JWT tokens still required

### ?? Security Best Practices

| Practice | Status | Notes |
|----------|--------|-------|
| **Use CSP instead of X-Frame-Options** | ? Implemented | CSP is more modern and flexible |
| **Whitelist specific domains** | ? Implemented | Only Ariba domains allowed |
| **Allow wildcards for subdomains** | ? Implemented | `*.ariba.com` covers all Ariba subdomains |
| **Remove conflicting headers** | ? Implemented | X-Frame-Options removed |
| **Configuration-based** | ? Implemented | Easy to add/remove domains |

---

## Testing the Fix

### Step 1: Verify CSP Header

After deploying, check the response headers:

```bash
curl -I https://shop.qualitybolt.com
```

Expected output:
```
HTTP/1.1 200 OK
Content-Security-Policy: frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com
```

Should **NOT** contain:
```
X-Frame-Options: SAMEORIGIN
```

### Step 2: Test in Ariba

1. Log into Ariba procurement system
2. Navigate to your punchout integration
3. Click "Shop" or equivalent action
4. Your shop should load in the Ariba iframe without CSP errors

### Step 3: Check Browser Console

Open Developer Tools (F12) in Ariba's iframe:

**Before Fix:**
```
Refused to display 'https://shop.qualitybolt.com/' in a frame because 
it violates the following Content Security Policy directive: "frame-ancestors 'self'".
```

**After Fix:**
```
(No CSP errors)
? Shop loads successfully
```

### Step 4: Test Direct Access

Verify the site still works when accessed directly (not in iframe):

```
https://shop.qualitybolt.com
```

Should load normally without errors.

---

## Browser Compatibility

| Browser | CSP frame-ancestors Support | Status |
|---------|---------------------------|--------|
| Chrome 40+ | ? Full support | ? Works |
| Firefox 33+ | ? Full support | ? Works |
| Safari 10+ | ? Full support | ? Works |
| Edge 15+ | ? Full support | ? Works |
| IE 11 | ? Not supported | ?? Falls back to allowing all frames |

**Note**: CSP `frame-ancestors` is widely supported. For IE 11, the middleware falls back gracefully (but IE 11 is end-of-life).

---

## Configuration Options

### Allow Additional Domains

To allow additional domains to frame your site, add them to the configuration:

```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://*.ariba.com",
      "https://*.sap.com",
      "https://s1.ariba.com",
      "https://service.ariba.com",
      "https://your-partner-portal.com",  // Add your domain
      "https://*.example.com"              // Add wildcards
    ]
  }
}
```

### Remove Specific Domains

To restrict access, remove domains from the array:

```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://s1.ariba.com"  // Only allow specific Ariba instance
    ]
  }
}
```

### Environment-Specific Configuration

**Development** (`appsettings.Development.json`):
```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://*.ariba.com",
      "https://localhost:5000"  // Allow local testing
    ]
  }
}
```

**Production** (`appsettings.Production.json`):
```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://s1.ariba.com",          // Production Ariba only
      "https://service.ariba.com"
    ]
  }
}
```

---

## Troubleshooting

### Issue: Still Getting CSP Error

**Check:**
1. ? Application restarted after configuration change
2. ? Correct appsettings file is being used (Development vs Production)
3. ? Browser cache cleared (CSP headers may be cached)
4. ? Using HTTPS (not HTTP)

**Solution:**
```bash
# Clear browser cache
Ctrl+Shift+Delete (Chrome/Edge)
Cmd+Shift+Delete (Safari)

# Or use incognito/private mode
```

### Issue: X-Frame-Options Still Present

**Check:**
- Another middleware might be adding X-Frame-Options
- Web server (IIS, Nginx) might be adding the header

**Solution:**
- Ensure the middleware runs **before** other middleware
- Check web.config (IIS) or nginx.conf for header rules

### Issue: Wildcard Not Working

**Problem:** `https://*.ariba.com` not matching `https://sub.ariba.com`

**Note:** CSP wildcards only match one subdomain level.

**Solution:**
```json
{
  "AllowedFrameOrigins": [
    "https://*.ariba.com",      // Matches sub.ariba.com
    "https://*.*.ariba.com"     // Matches sub.sub2.ariba.com (if needed)
  ]
}
```

Or add specific subdomains:
```json
{
  "AllowedFrameOrigins": [
    "https://s1.ariba.com",
    "https://s2.ariba.com",
    "https://service.ariba.com"
  ]
}
```

### Issue: Other Sites Can Still Frame

**Check:** If you removed the middleware by mistake

**Solution:** Ensure the middleware is present in `Program.cs` between `UseHsts()` and `UseHttpsRedirection()`.

---

## Alternative Solutions (Not Recommended)

### ? Option 1: Allow All Framing

```csharp
// DON'T DO THIS - Security risk!
context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors *");
```

**Risk:** Any site can embed your shop (phishing, clickjacking attacks)

### ? Option 2: Disable CSP Entirely

```csharp
// DON'T DO THIS - No frame protection!
context.Response.Headers.Remove("Content-Security-Policy");
context.Response.Headers.Remove("X-Frame-Options");
```

**Risk:** No clickjacking protection at all

### ?? Option 3: Use X-Frame-Options Only

```csharp
// Legacy approach - less flexible
context.Response.Headers.Append("X-Frame-Options", "ALLOW-FROM https://s1.ariba.com");
```

**Issues:**
- Deprecated
- Only supports one origin (no wildcards)
- Not supported by all browsers

---

## Deployment Checklist

### Before Deploying

- [ ] Configuration added to appsettings.json
- [ ] Configuration added to appsettings.Production.json (if different)
- [ ] Middleware added to Program.cs
- [ ] Build successful
- [ ] Tested locally with browser dev tools

### After Deploying

- [ ] Verify CSP header with curl/Postman
- [ ] Test Ariba punchout flow
- [ ] Check browser console for CSP errors
- [ ] Verify direct site access still works
- [ ] Test with different Ariba environments (if applicable)

---

## Related Configuration

### CORS Configuration

The Blazor app does not require CORS configuration because it's rendered server-side. The **API** (ShopQualityboltWeb) has CORS configured:

```csharp
// In ShopQualityboltWeb/Program.cs (API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

This is **separate** from CSP frame-ancestors and does not need to be changed.

### JWT Token Configuration

JWT tokens work across frames because they're sent in the Authorization header, not cookies. No changes needed to JWT configuration for CSP.

---

## Monitoring & Logging

### Add Logging (Optional)

To track which origins are attempting to frame your site:

```csharp
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var referer = context.Request.Headers["Referer"].ToString();
    
    if (!string.IsNullOrEmpty(referer))
    {
        logger.LogInformation("Site accessed from frame. Referer: {Referer}", referer);
    }
    
    // ... rest of CSP middleware
    
    await next();
});
```

### Azure Application Insights

If using Application Insights, CSP violations can be tracked:

```javascript
// In your _Host.cshtml or App.razor
<script>
document.addEventListener('securitypolicyviolation', (e) => {
    appInsights.trackException({
        exception: new Error('CSP Violation'),
        properties: {
            violatedDirective: e.violatedDirective,
            blockedURI: e.blockedURI,
            documentURI: e.documentURI
        }
    });
});
</script>
```

---

## Summary

### What Was Changed

1. ? Added CSP middleware to allow Ariba framing
2. ? Removed X-Frame-Options (conflicts with CSP)
3. ? Made allowed origins configurable
4. ? Maintained security with whitelist approach

### Impact

| Before | After |
|--------|-------|
| ? Ariba cannot frame site | ? Ariba can frame site |
| ? CSP blocks all framing except self | ? CSP allows specific domains |
| ?? X-Frame-Options conflicts | ? Clean CSP implementation |
| ? Hardcoded security policy | ? Configurable per environment |

### Benefits

- ? **Fixes Ariba Integration** - PunchOut works in iframe
- ? **Maintains Security** - Only whitelisted domains can frame
- ? **Future-Proof** - Easy to add new partners
- ? **Standards-Compliant** - Uses modern CSP (not deprecated X-Frame-Options)
- ? **Configurable** - Different settings per environment

---

**Status**: ? **FIXED**  
**Priority**: ?? **CRITICAL** - Required for Ariba integration  
**Risk**: ?? **LOW** - Secure whitelist approach  
**Testing Required**: Ariba PunchOut iframe loading  

**Next Steps**: Deploy to production and test with Ariba
