# ?? Troubleshooting Production Circuit Error

## Error You're Seeing

```
Error: There was an unhandled exception on the current circuit, so this circuit will be terminated.
```

This happens when clicking "Edit" on a user in User Management.

## Root Cause

The **AddIdentityRoles migration did not run** on production. The role tables (`AspNetRoles`, `AspNetUserRoles`, etc.) don't exist, causing errors when the API tries to fetch user roles.

---

## Immediate Diagnostic Steps

### Step 1: Check Migration Status

Run this PowerShell script:

```powershell
cd C:\Projects\QBECommerce_git\QBECommerce\ShopQualityboltWeb\ShopQualityboltWeb
.\CheckMigrationStatus.ps1
```

This will tell you:
- ? If roles table exists
- ? Which migrations are applied
- ? Which migrations are pending

### Step 2: Check Application Logs

**On the production server**, check:
- IIS logs: `C:\inetpub\logs\LogFiles\`
- Application Event Viewer: `eventvwr.msc`
- Application stdout logs (if configured)

Look for lines containing:
- "Starting database migration"
- "Applying pending migrations"
- "DATABASE MIGRATION FAILED"
- Any SQL errors

### Step 3: View Detailed Error

The error should now be visible because we enabled:
- `DetailedErrors: true` in both API and Blazor `appsettings.Production.json`
- Enhanced logging in `Program.cs`

After deploying, try editing a user again and you should see the actual error message.

---

## Solutions (Try in Order)

### Solution 1: Restart the Application (Quickest)

The migration code runs on startup. Sometimes it fails silently or the app pool hasn't restarted.

**IIS:**
```powershell
# In IIS Manager or PowerShell
Restart-WebAppPool -Name "YourAppPoolName"
```

**Or just restart the server** if you have access.

Wait 1-2 minutes, then run `CheckMigrationStatus.ps1` again.

---

### Solution 2: Manually Run the Migration

If automatic migration failed, run it manually:

#### Option A: Using dotnet ef (if you have access to the server)

```powershell
# On the production server
cd C:\path\to\deployed\application

dotnet ef database update --project QBExternalWebLibrary.dll --startup-project ShopQualityboltWeb.dll
```

#### Option B: Run the SQL Script Directly

The migration SQL script was already generated: `AddIdentityRoles.sql`

1. **Connect to production database** using SQL Server Management Studio
2. **Open** `AddIdentityRoles.sql`
3. **Execute** the script

The script is idempotent (safe to run multiple times).

---

### Solution 3: Check Database Permissions

The application identity needs permissions to:
- Create tables
- Create indexes
- Insert data

**Check connection string** in `appsettings.Production.json`:
```json
"DefaultConnectionString": "Server=QBEXTERNALWEB;Database=QBCommerceDB;Trusted_Connection=True;..."
```

**Verify the application pool identity** has:
- `db_owner` role on the database, OR
- At minimum: `db_datareader`, `db_datawriter`, `db_ddladmin`

---

### Solution 4: Check for Migration Errors in Startup

The improved `Program.cs` now logs detailed migration errors. After deploying:

1. **Restart the application**
2. **Check logs immediately** (first 30 seconds after restart)
3. Look for:
   ```
   Starting database migration...
   Applying X pending migrations: ...
   ```

If you see:
```
DATABASE MIGRATION FAILED - Application may not function correctly!
```

The log should have the actual error above it.

---

## What Changed to Help Debug

### 1. Detailed Errors Enabled

**API** (`ShopQualityboltWeb\appsettings.Production.json`):
```json
"DetailedErrors": true
```

**Blazor** (`ShopQualityboltWebBlazor\appsettings.Production.json`):
```json
"DetailedErrors": true,
"Logging": {
  "LogLevel": {
    "Default": "Information"  // Changed from Warning
  }
}
```

### 2. Enhanced Migration Logging

`Program.cs` now logs:
- Which migrations are being applied
- When roles are seeded
- Detailed errors if migration fails
- **Won't crash the app** if migration fails (so you can see the error)

### 3. Health Check Endpoint

`GET /api/bootstrap/health` now returns:
```json
{
  "database": {
    "pendingMigrations": [...],
    "appliedMigrationsCount": 16,
    "lastAppliedMigration": "20251107144854_AddIdentityRoles",
    "rolesTableExists": true
  }
}
```

---

## Expected Outcome After Fix

1. **Run CheckMigrationStatus.ps1**
   - ? `rolesTableExists: true`
   - ? `pendingMigrations: []` (empty)
   - ? `lastAppliedMigration: 20251107144854_AddIdentityRoles`

2. **Navigate to /admin/users**
   - ? Page loads
   - ? Users list shows with Roles column

3. **Click Edit on a user**
   - ? Dialog opens
   - ? Roles dropdown populated (Admin, User)
   - ? Can select roles and save

---

## Quick Reference Commands

```powershell
# Check migration status
.\CheckMigrationStatus.ps1

# Restart IIS app pool (replace name)
Restart-WebAppPool -Name "DefaultAppPool"

# View recent IIS logs
Get-ChildItem C:\inetpub\logs\LogFiles\W3SVC*\ -Recurse | 
  Sort-Object LastWriteTime -Descending | 
  Select-Object -First 1 | 
  Get-Content -Tail 50

# Manually run migration (on server)
dotnet ef database update
```

---

## Still Not Working?

If migrations won't run automatically:

1. **Use the manual SQL script**: `AddIdentityRoles.sql`
2. **Grant yourself admin** via User Management page
3. **Then remove the auto-migration code** from `Program.cs`
4. **Use manual migrations** going forward

This is actually the recommended approach for production anyway!

---

## After Everything Works

1. ? Verify roles exist: `/api/bootstrap/health`
2. ? Grant yourself Admin via User Management
3. ? Test at `/debug/my-roles`
4. ? Re-enable security (see `RE_ENABLE_SECURITY.md`)
5. ? **Disable DetailedErrors** in production appsettings
6. ? Clean up bootstrap files

---

**Deploy these changes and run `CheckMigrationStatus.ps1` to diagnose!**
