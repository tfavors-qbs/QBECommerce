# ?? Migration Status Debug Page - User Guide

## What Is This?

A **web-based diagnostic tool** that checks if database migrations have been applied on your production server - no server access required!

## How to Access

After deploying your application:

### Production:
```
https://shop.qualitybolt.com/debug/migration-status
```

### Development:
```
https://localhost:7169/debug/migration-status
```

Or simply **login** and click **"Migration Status"** in the navigation menu under "Debug Tools".

---

## What It Shows

The page displays:

### ? Health Check Results
- **Status** - Controller status
- **Environment** - Production/Development
- **Bootstrap Configured** - If bootstrap secret is set
- **Timestamp** - When the check was performed

### ? Database Status
- **Roles Table Exists** - ? Yes or ? No
- **Applied Migrations Count** - How many migrations have run
- **Last Applied Migration** - The most recent migration
- **Pending Migrations** - List of migrations that haven't run yet

---

## What Each Status Means

### ? All Green (Ready!)
```
Roles Table Exists: Yes ?
Pending Migrations: (none)
```
**Action**: Go to User Management and grant yourself admin!

### ?? Roles Table Missing
```
Roles Table Exists: No ?
```
**Problem**: The AddIdentityRoles migration didn't run

**Solutions**:
1. Ask server admin to restart the application
2. Check application logs for migration errors
3. Manually run the SQL migration script

### ?? Pending Migrations Found
```
Pending Migrations:
  - 20251107144854_AddIdentityRoles
```
**Problem**: Migrations exist but haven't been applied

**Solutions**:
1. Restart the application (migrations run on startup)
2. Check application logs for errors
3. Manually apply migrations

### ? Error Accessing Endpoint
```
Error: Failed to fetch
```
**Problem**: Can't connect to the health endpoint

**Possible Causes**:
- Application not running
- BootstrapAdminController not deployed
- Network/firewall issue
- Wrong API URL

---

## Step-by-Step Usage

### Step 1: Deploy Your Code
Deploy the updated code to production as normal.

### Step 2: Open the Debug Page
Navigate to:
```
https://shop.qualitybolt.com/debug/migration-status
```

Or click **"Migration Status"** in the nav menu after logging in.

### Step 3: Click "Check Migration Status"
The page will automatically check on load, but you can click the button to refresh.

### Step 4: Read the Results

#### If Everything is Green ?
- **Roles table exists**
- **No pending migrations**
- **"Database is ready!" message**

**? Proceed to User Management** to grant yourself admin

#### If There Are Issues ??
The page will show exactly what's wrong and suggest solutions.

Common issues:
- Migrations not applied ? Need to restart app or run manually
- Roles table missing ? Check logs, restart app
- Can't connect ? Application not running or not deployed

---

## Navigation Menu Changes

After deploying, you'll see a new section in the nav menu:

```
?? Catalog
????????????????????
?? Administration (TEMP: No Auth Required)
   ?? User Management

????????????????????
?? Debug Tools
   ?? Migration Status ? NEW!
   ?? My Roles
```

Both debug tools are available to any authenticated user (temporarily).

---

## What to Do Based on Results

### Scenario 1: Everything Works ?
```
Roles Table: Yes ?
Pending Migrations: None
```

**Next Steps:**
1. Click "Go to User Management"
2. Edit your user
3. Assign "Admin" role
4. Save
5. Verify at `/debug/my-roles`
6. Re-enable security (see RE_ENABLE_SECURITY.md)

---

### Scenario 2: Migrations Pending ??
```
Roles Table: No ?
Pending Migrations: AddIdentityRoles
```

**Next Steps:**
1. Contact server admin to restart the application
2. Wait 2-3 minutes for startup
3. Refresh the debug page
4. If still failing, ask admin to check logs
5. Last resort: Manually run `AddIdentityRoles.sql`

---

### Scenario 3: Can't Connect ?
```
Error: Failed to fetch
```

**Next Steps:**
1. Verify the application is running
2. Check deployment succeeded
3. Verify `BootstrapAdminController.cs` was deployed
4. Check firewall/network settings
5. Try the diagnostic PowerShell script: `CheckMigrationStatus.ps1`

---

## Advantages Over PowerShell Script

? **Works from anywhere** - No server access needed  
? **Visual interface** - Easy to read color-coded results  
? **Real-time** - Click refresh button anytime  
? **Always available** - Accessible via nav menu  
? **Mobile friendly** - Works on any device  
? **No installation** - Just open in browser  

---

## After Migrations Are Complete

Once you see ? and have granted yourself admin:

1. **Remove the debug page** (optional, but recommended for security):
   - Delete `DebugMigrationStatus.razor`
   - Remove the nav link from `NavMenu.razor`

2. **Re-enable security**:
   - Follow steps in `RE_ENABLE_SECURITY.md`

3. **Remove bootstrap files**:
   - Delete `BootstrapAdminController.cs`
   - Remove `BootstrapSecret` from appsettings

---

## Troubleshooting Tips

### Page loads but shows nothing
- Make sure you're logged in
- The page requires authentication

### "Check Migration Status" button does nothing
- Open browser DevTools (F12)
- Check Console for errors
- Verify API base URL is correct

### Shows wrong environment
- Check `ASPNETCORE_ENVIRONMENT` on server
- Verify correct appsettings file is being used

---

## Quick Reference

| Status | Meaning | Action |
|--------|---------|--------|
| ? Green checkmark | Working correctly | Proceed |
| ?? Yellow warning | Issue but not critical | Review details |
| ? Red X | Critical issue | Fix required |
| ?? Loading spinner | Checking status | Wait |

---

**This debug page gives you full visibility into migration status without needing server access!** ??
