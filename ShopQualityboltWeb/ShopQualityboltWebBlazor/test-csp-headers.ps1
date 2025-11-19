# CSP Header Test Script
# Run this to check if the CSP header is being set correctly

param(
    [string]$Url = "https://shop.qualitybolt.com"
)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "CSP Header Diagnostic Tool" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Testing URL: $Url" -ForegroundColor Yellow
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $Url -Method Head -UseBasicParsing -ErrorAction Stop
    
    Write-Host "? Request successful" -ForegroundColor Green
    Write-Host ""
    
    # Check Content-Security-Policy
    if ($response.Headers.ContainsKey("Content-Security-Policy")) {
        $csp = $response.Headers["Content-Security-Policy"]
        Write-Host "Content-Security-Policy:" -ForegroundColor Cyan
        Write-Host "  $csp" -ForegroundColor White
        Write-Host ""
        
        # Check if it allows Ariba
        if ($csp -match "frame-ancestors.*ariba") {
            Write-Host "? CSP allows Ariba framing" -ForegroundColor Green
        } else {
            Write-Host "? CSP does NOT allow Ariba framing" -ForegroundColor Red
            Write-Host "  Expected: frame-ancestors 'self' https://*.ariba.com ..." -ForegroundColor Yellow
            Write-Host "  Actual:   $csp" -ForegroundColor Yellow
        }
    } else {
        Write-Host "? No Content-Security-Policy header found" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # Check X-Frame-Options
    if ($response.Headers.ContainsKey("X-Frame-Options")) {
        $xfo = $response.Headers["X-Frame-Options"]
        Write-Host "? X-Frame-Options header found (should be removed):" -ForegroundColor Red
        Write-Host "  $xfo" -ForegroundColor White
        Write-Host "  This conflicts with CSP frame-ancestors" -ForegroundColor Yellow
    } else {
        Write-Host "? No X-Frame-Options header (good)" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "===========================================" -ForegroundColor Cyan
    Write-Host "All Response Headers:" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Cyan
    $response.Headers.GetEnumerator() | ForEach-Object {
        Write-Host "$($_.Key): $($_.Value)" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "? Error making request:" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Recommendations:" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

Write-Host "1. If CSP doesn't allow Ariba:" -ForegroundColor White
Write-Host "   - Check Program.cs has CSP middleware" -ForegroundColor Gray
Write-Host "   - Check appsettings.json has Ariba:AllowedFrameOrigins" -ForegroundColor Gray
Write-Host "   - Restart the application" -ForegroundColor Gray
Write-Host ""

Write-Host "2. If X-Frame-Options is present:" -ForegroundColor White
Write-Host "   - Check web.config removes X-Frame-Options" -ForegroundColor Gray
Write-Host "   - Check reverse proxy (IIS, Nginx) config" -ForegroundColor Gray
Write-Host ""

Write-Host "3. Clear caches:" -ForegroundColor White
Write-Host "   - Browser cache (Ctrl+Shift+Delete)" -ForegroundColor Gray
Write-Host "   - Server cache (restart app)" -ForegroundColor Gray
Write-Host "   - CDN cache (if using CloudFlare, etc.)" -ForegroundColor Gray
Write-Host ""

Write-Host "4. Test page:" -ForegroundColor White
Write-Host "   Visit: $Url/test-csp" -ForegroundColor Gray
Write-Host ""

Write-Host "For more help, see: CSP_STILL_BLOCKED_TROUBLESHOOTING.md" -ForegroundColor Cyan
