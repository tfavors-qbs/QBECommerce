# Check Production Database Migration Status
# Run this after deploying to verify migrations ran

Write-Host "=== Database Migration Diagnostic ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://shop.qualitybolt.com"

Write-Host "Checking migration status..." -ForegroundColor Yellow
Write-Host "URL: $baseUrl/api/bootstrap/health" -ForegroundColor Gray
Write-Host ""

try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/bootstrap/health" -Method Get
    
    Write-Host "? API is accessible" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "=== Health Check Results ===" -ForegroundColor Cyan
    Write-Host "Status: $($health.status)" -ForegroundColor White
    Write-Host "Environment: $($health.environment)" -ForegroundColor White
    Write-Host "Bootstrap Configured: $($health.bootstrapConfigured)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "=== Database Status ===" -ForegroundColor Cyan
    Write-Host "Roles Table Exists: $($health.database.rolesTableExists)" -ForegroundColor $(if ($health.database.rolesTableExists) { "Green" } else { "Red" })
    Write-Host "Applied Migrations Count: $($health.database.appliedMigrationsCount)" -ForegroundColor White
    Write-Host "Last Applied Migration: $($health.database.lastAppliedMigration)" -ForegroundColor White
    
    if ($health.database.pendingMigrations.Count -gt 0) {
        Write-Host ""
        Write-Host "??  PENDING MIGRATIONS FOUND!" -ForegroundColor Yellow
        Write-Host "The following migrations have NOT been applied:" -ForegroundColor Yellow
        $health.database.pendingMigrations | ForEach-Object {
            Write-Host "  - $_" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "ACTION REQUIRED:" -ForegroundColor Red
        Write-Host "1. Check application startup logs for migration errors" -ForegroundColor Gray
        Write-Host "2. Restart the application to trigger migrations" -ForegroundColor Gray
        Write-Host "3. Or manually run the migration SQL script" -ForegroundColor Gray
    }
    else {
        Write-Host ""
        Write-Host "? All migrations applied successfully!" -ForegroundColor Green
    }
    
    if (-not $health.database.rolesTableExists) {
        Write-Host ""
        Write-Host "? ROLES TABLE MISSING!" -ForegroundColor Red
        Write-Host "The AddIdentityRoles migration did not run successfully" -ForegroundColor Red
        Write-Host ""
        Write-Host "Possible causes:" -ForegroundColor Yellow
        Write-Host "1. Migration failed during startup (check logs)" -ForegroundColor Gray
        Write-Host "2. Database permissions issue" -ForegroundColor Gray
        Write-Host "3. Connection string incorrect" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Solutions:" -ForegroundColor Yellow
        Write-Host "1. Check application event logs or stdout logs" -ForegroundColor Gray
        Write-Host "2. Restart the application" -ForegroundColor Gray
        Write-Host "3. Manually run: dotnet ef database update" -ForegroundColor Gray
    }
}
catch {
    Write-Host "? ERROR accessing health endpoint" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "This means either:" -ForegroundColor Yellow
    Write-Host "• The application is not running" -ForegroundColor Gray
    Write-Host "• The BootstrapAdminController was not deployed" -ForegroundColor Gray
    Write-Host "• There's a network/firewall issue" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If roles table exists and no pending migrations:" -ForegroundColor Yellow
Write-Host "  ? Ready to grant admin access via /admin/users page" -ForegroundColor Green
Write-Host ""
Write-Host "If roles table missing or pending migrations found:" -ForegroundColor Yellow
Write-Host "  1. Check application logs for migration errors" -ForegroundColor Gray
Write-Host "  2. Restart the application/IIS pool" -ForegroundColor Gray
Write-Host "  3. If still failing, manually run the SQL migration script" -ForegroundColor Gray
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
