# CSP Still Blocking? Troubleshooting Guide

## You're Still Getting This Error:
```
Framing 'https://shop.qualitybolt.com/' violates the following Content Security Policy directive: 
"frame-ancestors 'self'". The request has been blocked.
```

## Quick Diagnosis Steps

### Step 1: Check the Actual Header Being Sent

Open your browser and navigate to:
```
https://shop.qualitybolt.com/test-csp
```

Then in browser DevTools (F12):
1. Go to **Network** tab
2. Refresh the page
3. Click on the page request
4. Look at **Response Headers**
5. Find `content-security-policy`

**What you SHOULD see:**
```
content-security-policy: frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com
```

**If you see this instead (WRONG):**
```
content-security-policy: frame-ancestors 'self'
```

Then something is overriding the header. Continue to Step 2.

### Step 2: Check for Reverse Proxy Override

#### Are you running behind IIS or Azure App Service?

**Check for web.config:**
- ? **We created** `ShopQualityboltWebBlazor/web.config` with header removal
- ? **Verify it's deployed** - Check your publish/deployment folder

**If web.config exists but still not working:**

Update your `web.config` to explicitly set the CSP header:

```xml
<configuration>
  <system.webServer>
    <httpProtocol>
      <customHeaders>
        <remove name="Content-Security-Policy" />
        <remove name="X-Frame-Options" />
        <add name="Content-Security-Policy" value="frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

#### Are you running behind Nginx?

Check your Nginx config for `add_header` directives:

```nginx
# WRONG - This overrides ASP.NET headers
add_header Content-Security-Policy "frame-ancestors 'self'";

# RIGHT - Let ASP.NET handle it, or set it in Nginx:
add_header Content-Security-Policy "frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com";
```

Or remove the `add_header Content-Security-Policy` line entirely and let ASP.NET Core handle it.

#### Are you using Azure Front Door, CloudFlare, or another WAF/CDN?

These services can add or override headers. Check:

**Azure Front Door:**
- Go to Rules Engine
- Check for any CSP header rules
- Remove or update them

**CloudFlare:**
- Go to Security ? WAF ? Custom Rules
- Check Transform Rules
- Disable or update CSP rules

**Cloudflare Workers:**
- Check for any Workers that modify headers
- Update or disable them

### Step 3: Clear All Caches

Even with the right configuration, old headers might be cached:

```bash
# 1. Clear Browser Cache
Ctrl+Shift+Delete (Chrome/Edge)
Cmd+Shift+Delete (Mac)

# 2. Use Incognito/Private Mode
Ctrl+Shift+N (Chrome/Edge)
Cmd+Shift+P (Safari)

# 3. Clear Server Cache (if applicable)
# Azure App Service: Restart the app
# IIS: Run "iisreset" or restart app pool
# Nginx: "nginx -s reload"

# 4. Clear CDN Cache (if using one)
# CloudFlare: Purge Everything
# Azure CDN: Purge specific URL
```

### Step 4: Test with curl

From your local machine or server, run:

```bash
curl -I https://shop.qualitybolt.com
```

Look for:
```
content-security-policy: frame-ancestors 'self' https://*.ariba.com ...
```

If you see `frame-ancestors 'self'` only, the header is being overridden somewhere.

### Step 5: Check Application Logs

Look for any errors in your application logs about CSP configuration:

**Development:**
```bash
# Check Visual Studio Output window
# Or console where you run "dotnet run"
```

**Production:**
```bash
# Azure App Service: App Service Logs
# IIS: Event Viewer ? Windows Logs ? Application
```

---

## Common Issues & Solutions

### Issue 1: Running on Azure App Service

**Problem:** Azure adds default security headers

**Solution:** Use the `web.config` file we created, and:

1. **Verify it's in the published output:**
   ```
   publish/web.config
   ```

2. **Or use Azure Configuration:**
   - Go to Azure Portal ? Your App Service
   - Configuration ? Application Settings
   - Add: `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` = `Microsoft.Azure.AppService.Proxy.Client`

3. **Restart the App Service**

### Issue 2: Middleware Order

**Problem:** HSTS is adding CSP before our middleware

**Solution:** ? **Already fixed** - Our CSP middleware now runs BEFORE HSTS using `OnStarting`

Verify in `Program.cs`:
```csharp
// CSP middleware FIRST
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() => { /* CSP logic */ });
    await next();
});

// HSTS after
app.UseHsts();
```

### Issue 3: Headers Not Overwriting

**Problem:** Using `Append` instead of setting the header

**Solution:** ? **Already fixed** - We now use:
```csharp
context.Response.Headers.Remove("Content-Security-Policy");
context.Response.Headers["Content-Security-Policy"] = "..."; // Sets, not appends
```

### Issue 4: Configuration Not Loading

**Problem:** `Ariba:AllowedFrameOrigins` not in appsettings.json

**Solution:** ? **Should be fixed** - But verify:

**Check `appsettings.json`:**
```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://*.ariba.com",
      "https://*.sap.com",
      "https://s1.ariba.com",
      "https://service.ariba.com"
    ]
  }
}
```

**Test by visiting:**
```
https://shop.qualitybolt.com/test-csp
```

This page shows the loaded configuration.

### Issue 5: Wildcard Domains Not Working

**Problem:** Browser doesn't support wildcards in CSP

**Solution:** CSP Level 3 supports wildcards, but if it still doesn't work:

**Option A: Add specific subdomains**
```json
{
  "AllowedFrameOrigins": [
    "https://s1.ariba.com",
    "https://s2.ariba.com",
    "https://s3.ariba.com",
    "https://service.ariba.com",
    "https://supplier.ariba.com"
  ]
}
```

**Option B: Use scheme-only**
```csharp
// In Program.cs, change to:
context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self' https://*.ariba.com https:";
```

?? **Warning:** `https:` allows ALL HTTPS sites to frame you. Only use if necessary.

---

## Testing Checklist

After making changes:

- [ ] Restart the application
- [ ] Clear browser cache
- [ ] Test in Incognito/Private mode
- [ ] Visit `/test-csp` page to verify configuration
- [ ] Check response headers with curl or browser DevTools
- [ ] Test Ariba iframe loading
- [ ] Check for errors in application logs

---

## Still Not Working?

### Enable Debug Logging

Add this to `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

This will show more details about headers being set.

### Add Console Logging to Middleware

Temporarily add logging to the CSP middleware:

```csharp
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        
        // Log what we're setting
        var frameAncestors = "..."; // your logic
        logger.LogWarning("Setting CSP header: frame-ancestors {FrameAncestors}", frameAncestors);
        
        context.Response.Headers["Content-Security-Policy"] = $"frame-ancestors {frameAncestors}";
        
        // Log what was actually set
        logger.LogWarning("CSP header value: {CSP}", context.Response.Headers["Content-Security-Policy"]);
        
        return Task.CompletedTask;
    });
    
    await next();
});
```

### Contact Support

If nothing works, provide this information:

1. **Hosting Environment:** Azure, AWS, IIS, Nginx, other?
2. **Response Headers:** Output from `curl -I https://shop.qualitybolt.com`
3. **Application Logs:** Any CSP-related errors
4. **Browser:** Chrome, Firefox, Safari, Edge?
5. **Screenshot:** Browser DevTools Network tab showing the CSP header

---

## Quick Reference

### Expected Header Value:
```
content-security-policy: frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com
```

### Should NOT See:
```
content-security-policy: frame-ancestors 'self'
x-frame-options: SAMEORIGIN
x-frame-options: DENY
```

### Test URLs:
- **CSP Test Page:** `https://shop.qualitybolt.com/test-csp`
- **Home Page:** `https://shop.qualitybolt.com`
- **Login Page:** `https://shop.qualitybolt.com/login`

### Files to Check:
1. `ShopQualityboltWebBlazor/Program.cs` - CSP middleware
2. `ShopQualityboltWebBlazor/appsettings.json` - Configuration
3. `ShopQualityboltWebBlazor/web.config` - IIS/Azure overrides
4. Server config (nginx.conf, IIS settings, etc.)

---

**Last Updated:** After implementing OnStarting fix and web.config  
**Status:** Should work now - If not, follow this guide  
**Priority:** ?? CRITICAL for Ariba integration
