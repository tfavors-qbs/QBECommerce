# Quick Fix: Ariba CSP Frame Error

## The Error
```
Framing 'https://shop.qualitybolt.com/' violates the following Content Security Policy directive: 
"frame-ancestors 'self'". The request has been blocked.
```

## The Cause
Your Blazor app was blocking Ariba from loading it in an iframe due to restrictive Content-Security-Policy headers.

## The Fix
? **Added middleware** to allow Ariba domains to frame your site  
? **Removed X-Frame-Options** header (conflicts with CSP)  
? **Made it configurable** via appsettings.json  

## Files Changed
1. `ShopQualityboltWebBlazor/Program.cs` - Added CSP middleware
2. `ShopQualityboltWebBlazor/appsettings.json` - Added Ariba origins
3. `ShopQualityboltWebBlazor/appsettings.Development.json` - Added Ariba origins

## Test It
After deploying, verify the header:
```bash
curl -I https://shop.qualitybolt.com
```

Should see:
```
Content-Security-Policy: frame-ancestors 'self' https://*.ariba.com https://*.sap.com https://s1.ariba.com https://service.ariba.com
```

Should **NOT** see:
```
X-Frame-Options: SAMEORIGIN
```

## What Domains Are Allowed
- `https://*.ariba.com` - All Ariba subdomains
- `https://*.sap.com` - All SAP subdomains  
- `https://s1.ariba.com` - Specific Ariba instance
- `https://service.ariba.com` - Ariba service domain
- `'self'` - Your own domain

## Add More Domains
Edit `appsettings.json`:
```json
{
  "Ariba": {
    "AllowedFrameOrigins": [
      "https://*.ariba.com",
      "https://your-new-domain.com"
    ]
  }
}
```

## Security
? Only whitelisted domains can frame your site  
? Prevents clickjacking from unauthorized sites  
? Modern CSP standard (better than old X-Frame-Options)  

## See Full Documentation
Read `ARIBA_CSP_FRAME_ANCESTORS_FIX.md` for complete details.

---

**Status**: ? FIXED  
**Deploy**: Ready for production  
**Test**: Try Ariba PunchOut after deploying
