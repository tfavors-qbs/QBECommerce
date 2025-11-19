# IMMEDIATE ACTIONS - CSP Still Blocking

## Current Status
? Still getting: `"frame-ancestors 'self'"` error when Ariba tries to frame your site

## What We Just Fixed

1. ? **Updated CSP middleware** - Now uses `OnStarting` to prevent HSTS override
2. ? **Added web.config** - Removes conflicting headers from IIS/Azure
3. ? **Added test page** - `/test-csp` for diagnostics
4. ? **Added test script** - PowerShell script to check headers

## Do This NOW (In Order)

### 1. Restart Your Application

**If running locally:**
```bash
# Stop the app (Ctrl+C)
# Clean and rebuild
dotnet clean
dotnet build
dotnet run
```

**If running on Azure/Production:**
- Go to Azure Portal ? Your App Service ? Restart

### 2. Clear Your Browser Cache

- **Chrome/Edge:** Ctrl+Shift+Delete ? Clear cached images and files
- **Or:** Open in Incognito/Private mode (Ctrl+Shift+N)

### 3. Test the Header

**Option A: Use the PowerShell script**
```powershell
cd ShopQualityboltWebBlazor
.\test-csp-headers.ps1 -Url "https://shop.qualitybolt.com"
```

**Option B: Use curl**
```bash
curl -I https://shop.qualitybolt.com
```

**Option C: Visit the test page**
```
https://shop.qualitybolt.com/test-csp
```

Then:
1. Open DevTools (F12)
2. Network tab
3. Refresh page
4. Click on the request
5. Look at Response Headers
6. Find `content-security-policy`

### 4. What Should You See?

**? CORRECT (Good):**
```
content-security-policy: frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com
```

**? ALSO CORRECT (No X-Frame-Options):**
```
(X-Frame-Options header should NOT be present)
```

**? WRONG (Bad):**
```
content-security-policy: frame-ancestors 'self'
```

or

```
x-frame-options: SAMEORIGIN
```

---

## If Still Showing Wrong Headers

### Check 1: Is web.config deployed?

Look in your published/deployed folder for:
```
ShopQualityboltWebBlazor/web.config
```

If missing, publish again:
```bash
dotnet publish -c Release
```

### Check 2: Are you behind a reverse proxy?

**IIS:** Check IIS Manager for custom headers  
**Nginx:** Check `/etc/nginx/nginx.conf` for `add_header` directives  
**Azure Front Door/CloudFlare:** Check for header rules  

### Check 3: Update web.config to explicitly set CSP

If the current web.config isn't working, update it to:

```xml
<configuration>
  <system.webServer>
    <httpProtocol>
      <customHeaders>
        <remove name="Content-Security-Policy" />
        <remove name="X-Frame-Options" />
        <add name="Content-Security-Policy" 
             value="frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

Then redeploy and restart.

---

## If Headers Are Correct But Ariba Still Blocks

### Issue: Browser Not Accepting Wildcards

Some browsers might not support `https://*.ariba.com` syntax.

**Solution:** List specific subdomains in `appsettings.json`:

```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://s1.ariba.com",
      "https://s2.ariba.com",
      "https://s3.ariba.com",
      "https://s4.ariba.com",
      "https://service.ariba.com",
      "https://supplier.ariba.com",
      "https://buyer.ariba.com"
    ]
  }
}
```

Ask Ariba support which specific domains they use.

### Issue: Ariba Using Different Domain

Check the error message carefully. It should show the domain trying to frame your site:

```
Framing 'https://shop.qualitybolt.com/' from 'https://XXXXX.ariba.com' violates...
```

Add that specific domain to your allowed list.

---

## Quick Diagnostic Commands

### Test from Command Line
```bash
# Windows PowerShell
.\test-csp-headers.ps1

# Mac/Linux
curl -I https://shop.qualitybolt.com | grep -i "content-security-policy"
```

### Test in Browser Console
```javascript
fetch('https://shop.qualitybolt.com')
  .then(r => {
    console.log('CSP:', r.headers.get('content-security-policy'));
    console.log('XFO:', r.headers.get('x-frame-options'));
  });
```

---

## Contact Info

If none of this works, provide these details:

1. **Output from test script** (or curl command)
2. **Screenshot of browser DevTools** (Network tab, Response Headers)
3. **Hosting environment** (Azure, IIS, Nginx, etc.)
4. **Any error messages** in application logs
5. **Exact Ariba domain** trying to frame your site

---

## Files You Need to Check

1. ? `ShopQualityboltWebBlazor/Program.cs` - CSP middleware (updated)
2. ? `ShopQualityboltWebBlazor/appsettings.json` - Configuration (updated)
3. ? `ShopQualityboltWebBlazor/web.config` - IIS overrides (new)
4. ? Server/reverse proxy config (check for header overrides)

---

## Success Criteria

You'll know it's working when:

1. ? `/test-csp` page shows correct configuration
2. ? `curl -I` shows CSP with Ariba domains
3. ? No X-Frame-Options header
4. ? Ariba can load your site in iframe without CSP error
5. ? Browser console shows no CSP violations

---

**Next Step:** Run the test script or visit `/test-csp` and report what you see.
