# Quick Test Script - Grant Admin to Yourself
# Run this in PowerShell

# For LOCAL development (localhost)
$localBody = @{
    email = "tfavors@qualitybolt.com"
    secret = "TempAdminBootstrap2024!"
} | ConvertTo-Json

Write-Host "Granting Admin role to tfavors@qualitybolt.com..." -ForegroundColor Yellow

try {
    $result = Invoke-RestMethod `
        -Uri "https://localhost:7000/api/bootstrap/grant-admin" `
        -Method Post `
        -Body $localBody `
        -ContentType "application/json" `
        -SkipCertificateCheck

    Write-Host "? SUCCESS!" -ForegroundColor Green
    Write-Host "Message: $($result.message)" -ForegroundColor Green
    Write-Host "Email: $($result.email)" -ForegroundColor Cyan
    Write-Host "Roles: $($result.roles -join ', ')" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "?? You can now access the admin panel at /admin/users" -ForegroundColor Green
    Write-Host "??  Remember to delete BootstrapAdminController.cs after this!" -ForegroundColor Yellow
}
catch {
    Write-Host "? ERROR!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Make sure the API is running on port 7000" -ForegroundColor Gray
    Write-Host "2. Check that your user account exists (tfavors@qualitybolt.com)" -ForegroundColor Gray
    Write-Host "3. Verify BootstrapSecret in appsettings.json" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
