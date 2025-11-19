# Troubleshooting: "Failed to load shopping carts" Error

## Common Issues and Solutions

### 1. User Doesn't Have QBSales Role
**Symptom**: HTTP 403 Forbidden error when accessing `/api/qbsales/carts`

**Solution**:
1. Navigate to **Debug Tools ? My Roles** to check your current roles
2. If you don't see "QBSales" or "Admin", you need to assign the role
3. Admin users can assign roles via **Admin ? User Management**

#### Assign QBSales Role:
```sql
-- Option 1: Via SQL (if you have database access)
-- First, get the role ID
SELECT Id FROM AspNetRoles WHERE Name = 'QBSales';

-- Then assign to user (replace with actual IDs)
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES ('your-user-id', 'qbsales-role-id');
```

#### Or via Admin UI:
1. Login as an Admin user
2. Go to **Admin ? User Management**
3. Edit the user who needs QBSales access
4. Add "QBSales" to their roles
5. Save changes
6. User needs to log out and log back in

### 2. QBSales Role Hasn't Been Created
**Symptom**: Role doesn't exist in database

**Solution**: Restart the application. The role is auto-created on startup via `Program.cs`:
```csharp
var roles = new[] { "Admin", "User", "QBSales" };
```

Check application logs for:
- "Seeding roles..."
- "Creating role: QBSales"
- "Role seeding completed successfully"

### 3. Authentication Token Issue
**Symptom**: 401 Unauthorized

**Solution**:
1. Log out and log back in
2. Check browser console for authentication errors
3. Clear browser cookies/cache
4. Check that JWT token is being sent in requests

### 4. CORS or API Connection Issue
**Symptom**: Network error, CORS error in console

**Solution**:
1. Verify Blazor app is running on expected port
2. Check `appsettings.json` for correct API URL
3. Ensure both Blazor and API projects are running
4. Check browser console for CORS errors

### 5. Navigation Property Not Loading
**Symptom**: Carts show but ClientName is null

**Solution**: This has been fixed in the latest code. `DataContext` now properly loads the `Client` navigation property using `.Include()`.

## Quick Diagnostic Steps

### Step 1: Check Your Roles
Navigate to: `/debug/my-roles`
- Should show: "QBSales" or "Admin"
- If missing: Request role assignment from admin

### Step 2: Check Browser Console
Open Developer Tools (F12):
- Look for HTTP errors (401, 403, 500)
- Check Network tab for failed requests
- Look for authentication/CORS errors

### Step 3: Check API Logs
Look for errors in API output window:
- Authorization failures
- Database errors
- Null reference exceptions

### Step 4: Test API Endpoint Directly
Using browser or Postman:
```
GET https://localhost:7000/api/qbsales/carts
Authorization: Bearer {your-jwt-token}
```

Expected responses:
- **200 OK**: Success, returns cart array
- **401 Unauthorized**: Not authenticated
- **403 Forbidden**: Missing QBSales/Admin role
- **500 Internal Server Error**: Server-side error

## Error Messages and Meanings

| Error Message | Status Code | Meaning | Solution |
|--------------|-------------|---------|----------|
| "Failed to load shopping carts" | 403 | Missing QBSales/Admin role | Assign role to user |
| "Failed to load shopping carts" | 401 | Not authenticated | Log in again |
| "Failed to load shopping carts" | 500 | Server error | Check API logs |
| "Error loading data: [message]" | N/A | Network/exception | Check console/network |

## Verification Checklist

- [ ] Application has been restarted (to seed QBSales role)
- [ ] User has QBSales or Admin role assigned
- [ ] User has logged out and back in after role assignment
- [ ] Both API and Blazor projects are running
- [ ] No errors in browser console
- [ ] No errors in API output window
- [ ] Can access `/debug/my-roles` page
- [ ] Other API endpoints work (e.g., `/api/clients`)

## Testing the Fix

### Test 1: Check Role Seeding
1. Restart the API application
2. Check output logs for "Creating role: QBSales"
3. Verify in database: `SELECT * FROM AspNetRoles WHERE Name = 'QBSales'`

### Test 2: Assign and Test Role
1. Assign QBSales role to test user
2. User logs out and back in
3. Navigate to `/qbsales/cart-management`
4. Should load successfully

### Test 3: Verify Data Loading
1. Select a client from dropdown
2. Should see carts for that client (or message if none)
3. Client name column should be populated
4. "Create Cart" button should appear

## Still Having Issues?

### Enable Detailed Logging
In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "ShopQualityboltWeb.Controllers.Api.QBSalesCartController": "Debug"
    }
  }
}
```

### Check Database Directly
```sql
-- Check if roles exist
SELECT * FROM AspNetRoles;

-- Check user roles
SELECT u.Email, r.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id;

-- Check shopping carts
SELECT sc.Id, sc.ApplicationUserId, u.Email, u.ClientId, c.Name as ClientName
FROM ShoppingCarts sc
JOIN AspNetUsers u ON sc.ApplicationUserId = u.Id
LEFT JOIN Clients c ON u.ClientId = c.Id;
```

### Contact Support
If none of the above works, provide:
1. HTTP status code from error message
2. Browser console errors
3. API output window logs
4. Result of `/debug/my-roles` page
5. Database query results from above

---

**Most Common Fix**: User needs QBSales role assigned and must log out/in after assignment.
