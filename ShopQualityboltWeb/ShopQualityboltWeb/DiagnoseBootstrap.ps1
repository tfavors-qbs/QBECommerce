# Diagnose Bootstrap Endpoint on Production
# This script tests various endpoints to identify the issue

Write-Host "=== Bootstrap Endpoint Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://shop.qualitybolt.com"

# Test 1: Health Check
Write-Host "Test 1: Health Check Endpoint" -ForegroundColor Yellow
Write-Host "Testing: GET $baseUrl/api/bootstrap/health" -ForegroundColor Gray
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/bootstrap/health" -Method Get
    Write-Host "? SUCCESS - Controller is deployed!" -ForegroundColor Green
    Write-Host "Status: $($health.status)" -ForegroundColor Cyan
    Write-Host "Environment: $($health.environment)" -ForegroundColor Cyan
    Write-Host "Bootstrap Configured: $($health.bootstrapConfigured)" -ForegroundColor Cyan
    Write-Host "Timestamp: $($health.timestamp)" -ForegroundColor Cyan
}
catch {
    Write-Host "? FAILED - Controller not found or not accessible" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response.StatusCode) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Test 2: Try the grant-admin endpoint
Write-Host "Test 2: Grant Admin Endpoint" -ForegroundColor Yellow
Write-Host "Testing: POST $baseUrl/api/bootstrap/grant-admin" -ForegroundColor Gray

$body = @{
    email = "tfavors@qualitybolt.com"
    secret = "TempAdminBootstrap2024!"
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/bootstrap/grant-admin" -Method Post -Body $body -ContentType "application/json"
    Write-Host "? SUCCESS!" -ForegroundColor Green
    Write-Host "Message: $($result.message)" -ForegroundColor Cyan
    Write-Host "Email: $($result.email)" -ForegroundColor Cyan
    Write-Host "Roles: $($result.roles -join ', ')" -ForegroundColor Cyan
}
catch {
    Write-Host "? FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Yellow
        
        if ($_.Exception.Response.StatusCode) {
            Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Test 3: Check if API base is accessible
Write-Host "Test 3: API Base Accessibility" -ForegroundColor Yellow
Write-Host "Testing: GET $baseUrl/api/" -ForegroundColor Gray
try {
    $swagger = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -UseBasicParsing
    Write-Host "? Swagger UI is accessible" -ForegroundColor Green
}
catch {
    Write-Host "??  Swagger UI not accessible (might be disabled in production)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Test 4: Test accounts endpoint as a comparison
Write-Host "Test 4: Compare with Accounts Endpoint (known working)" -ForegroundColor Yellow
Write-Host "Testing: GET $baseUrl/api/accounts/info" -ForegroundColor Gray
try {
    $accounts = Invoke-RestMethod -Uri "$baseUrl/api/accounts/info" -Method Get
    Write-Host "? Accounts endpoint is working" -ForegroundColor Green
}
catch {
    Write-Host "??  Accounts endpoint requires authentication (expected)" -ForegroundColor Yellow
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "? This is normal - endpoint exists and is protected" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Diagnostic Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If the health check failed (404):" -ForegroundColor Yellow
Write-Host "1. The BootstrapAdminController.cs file wasn't deployed" -ForegroundColor Gray
Write-Host "2. Check your deployment process includes all .cs files" -ForegroundColor Gray
Write-Host "3. Verify the build includes the new controller" -ForegroundColor Gray
Write-Host "4. Try restarting the IIS application pool" -ForegroundColor Gray
Write-Host ""
Write-Host "If the health check succeeded but grant-admin failed:" -ForegroundColor Yellow
Write-Host "1. Check the BootstrapSecret in appsettings.Production.json" -ForegroundColor Gray
Write-Host "2. Verify the user account exists in the database" -ForegroundColor Gray
Write-Host "3. Check the error message above for specific details" -ForegroundColor Gray
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
