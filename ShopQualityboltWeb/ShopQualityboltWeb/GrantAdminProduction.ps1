# Grant Admin to Production Server
# Run this in PowerShell

# For PRODUCTION (shop.qualitybolt.com)
$prodBody = @{
    email = "tfavors@qualitybolt.com"
    secret = "TempAdminBootstrap2024!"
} | ConvertTo-Json

Write-Host "Granting Admin role to tfavors@qualitybolt.com on PRODUCTION..." -ForegroundColor Yellow
Write-Host ""

try {
    $result = Invoke-RestMethod `
        -Uri "https://shop.qualitybolt.com/api/bootstrap/grant-admin" `
        -Method Post `
        -Body $prodBody `
        -ContentType "application/json"

    Write-Host "? SUCCESS!" -ForegroundColor Green
    Write-Host "Message: $($result.message)" -ForegroundColor Green
    Write-Host "Email: $($result.email)" -ForegroundColor Cyan
    Write-Host "Roles: $($result.roles -join ', ')" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "?? You can now access the admin panel at https://shop.qualitybolt.com/admin/users" -ForegroundColor Green
    Write-Host ""
    Write-Host "??  IMPORTANT: After confirming admin access works:" -ForegroundColor Yellow
    Write-Host "   1. Delete BootstrapAdminController.cs" -ForegroundColor Red
    Write-Host "   2. Remove BootstrapSecret from appsettings.Production.json" -ForegroundColor Red
    Write-Host "   3. Redeploy the application" -ForegroundColor Red
}
catch {
    Write-Host "? ERROR!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Make sure the API is deployed and running" -ForegroundColor Gray
    Write-Host "2. Check that your user account exists (tfavors@qualitybolt.com)" -ForegroundColor Gray
    Write-Host "3. Verify BootstrapSecret in appsettings.Production.json" -ForegroundColor Gray
    Write-Host "4. Make sure migrations have run (roles exist)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
