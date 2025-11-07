# Troubleshooting: 404 Error on Production Bootstrap Endpoint

## Problem
- ? Endpoint works on localhost (debugging)
- ? 404 error on production (https://shop.qualitybolt.com)

## Step-by-Step Diagnosis

### Step 1: Run the Diagnostic Script

```powershell
cd C:\Projects\QBECommerce_git\QBECommerce\ShopQualityboltWeb\ShopQualityboltWeb
.\DiagnoseBootstrap.ps1
```

This will test multiple endpoints and tell you exactly what's wrong.

### Step 2: Check If File Was Deployed

**Common Issue:** The new `BootstrapAdminController.cs` file wasn't deployed to production.

**How to verify:**
1. Check your deployment package/folder
2. Look for: `Controllers\Api\BootstrapAdminController.cs` or the compiled DLL

**Fix:**
- Rebuild the project
- Ensure your deployment process includes all new files
- Redeploy

### Step 3: Verify the Controller is Compiled

Run this command to verify the controller compiles:

```powershell
dotnet build ShopQualityboltWeb\ShopQualityboltWeb.csproj --configuration Release
```

Look for any compilation errors.

### Step 4: Check IIS/Hosting Configuration

If you're using IIS:

1. **Restart the Application Pool**
   ```powershell
   # In IIS Manager or via PowerShell
   Restart-WebAppPool -Name "YourAppPoolName"
   ```

2. **Check the deployed files**
   - Navigate to your deployment directory on the server
   - Look for `ShopQualityboltWeb.dll` in the bin folder
   - Check the file date to ensure it's the latest version

3. **Verify web.config**
   - Ensure `<aspNetCore>` element is present
   - Check `processPath` points to correct dotnet executable

### Step 5: Test the Health Endpoint

After deploying, test this URL first:

```
https://shop.qualitybolt.com/api/bootstrap/health
```

**Expected Response:**
```json
{
  "status": "Bootstrap controller is active",
  "timestamp": "2024-11-07T15:00:00Z",
  "environment": "Production",
  "bootstrapConfigured": true
}
```

**If you get 404:**
- Controller wasn't deployed
- Application hasn't restarted
- Route configuration issue

### Step 6: Check Application Logs

Look at your production logs for:
- Startup errors
- Controller discovery issues
- Migration errors

**Common locations:**
- IIS: `C:\inetpub\logs\LogFiles\`
- Application logs: Check your logging provider
- Event Viewer: Windows Logs > Application

### Step 7: Verify appsettings.Production.json

Make sure `BootstrapSecret` is in the deployed `appsettings.Production.json`:

```json
{
  ...
  "BootstrapSecret": "TempAdminBootstrap2024!"
}
```

### Step 8: Manual Deployment Checklist

If automated deployment is failing, try manual deployment:

1. **Build in Release mode:**
   ```powershell
   dotnet publish ShopQualityboltWeb\ShopQualityboltWeb.csproj -c Release -o C:\Temp\Publish
   ```

2. **Verify BootstrapAdminController.dll is in the output:**
   - Check `C:\Temp\Publish\ShopQualityboltWeb.dll`
   - Use a DLL inspector or decompiler to verify the controller is included

3. **Copy to production server**

4. **Restart the application**

### Step 9: Alternative - Use Swagger to Verify

If Swagger is enabled in production, navigate to:
```
https://shop.qualitybolt.com/swagger/index.html
```

Look for the Bootstrap endpoints under the "Bootstrap" section.

### Step 10: Nuclear Option - Rebuild Everything

```powershell
# Clean everything
dotnet clean ShopQualityboltWeb\ShopQualityboltWeb.csproj

# Restore packages
dotnet restore ShopQualityboltWeb\ShopQualityboltWeb.csproj

# Build
dotnet build ShopQualityboltWeb\ShopQualityboltWeb.csproj --configuration Release

# Publish
dotnet publish ShopQualityboltWeb\ShopQualityboltWeb.csproj -c Release -o C:\Temp\Publish

# Deploy to production
# (Your deployment process here)

# Restart application on server
```

## Quick Wins to Try First

1. **Restart the app on the server** (simplest fix)
2. **Check if BootstrapAdminController.cs was committed to Git:**
   ```powershell
   git status
   git log --oneline --name-only -1
   ```
3. **Verify the file exists in your deployment package**

## Alternative Solution: Use Swagger Endpoint

If you have Swagger enabled, you can test directly:

1. Navigate to: `https://shop.qualitybolt.com/swagger/index.html`
2. Find "Bootstrap" section
3. Use "Try it out" to call the endpoint

## Last Resort: Direct Database Access

If you absolutely can't get the endpoint working, you can:

1. Ask someone with database access to run the SQL
2. Use Azure Data Studio with connection tunneling
3. Set up a VPN to the production database

---

## After You Fix It

Once working, the sequence is:

1. Test health endpoint ? ?
2. Grant admin role ? ?
3. Login and verify ? ?
4. Delete BootstrapAdminController.cs ? ?
5. Remove BootstrapSecret ? ?
6. Redeploy ? ?
