# User Management Admin System - Deployment Guide

## Summary of Changes

### Database Migrations
- **Migration**: `20251107144854_AddIdentityRoles`
- **Purpose**: Adds ASP.NET Core Identity role tables (AspNetRoles, AspNetUserRoles, AspNetRoleClaims)
- **Status**: ? Migration will be **automatically applied** on application startup

### Features Added

#### Backend (ShopQualityboltWeb API)
1. **UsersController.cs** - Full CRUD API for user management
   - `GET /api/users` - List all users
   - `GET /api/users/{id}` - Get specific user
   - `GET /api/users/roles` - Get available roles
   - `POST /api/users` - Create user (with role assignment)
   - `PUT /api/users/{id}` - Update user (including roles)
   - `DELETE /api/users/{id}` - Delete user
   - All endpoints require Admin role

2. **Role Management**
   - Roles automatically seeded: "Admin", "User"
   - Role-based authorization configured
   - Automatic migration on startup

#### Service Layer (QBExternalWebLibrary)
1. **UserApiService.cs** - HTTP client service for user management
2. **UserViewModel, CreateUserRequest, UpdateUserRequest** - DTOs with role support

#### Frontend (ShopQualityboltWebBlazor)
1. **AdminUsers.razor** - Main admin page
   - User list with roles, status, and client info
   - Create/Edit/Delete functionality
   - Access control with helpful error messages

2. **UserDialog.razor** - Create/Edit user dialog
   - All user fields (Email, Password, Names, Ariba ID, Client)
   - Multi-select role assignment
   - Client dropdown

3. **Navigation** - Admin section in NavMenu (visible only to Admins)

4. **Debug Pages**
   - `/debug/my-roles` - Check your current roles and claims
   - `/access-denied` - User-friendly access denied page

## Deployment Steps

### 1. Deploy Code to Server
Push your code to GitHub and deploy to your server as normal.

### 2. The Migration Happens Automatically
When you **start the application**, it will:
1. ? Check for pending migrations
2. ? Apply the `AddIdentityRoles` migration
3. ? Create "Admin" and "User" roles
4. ? Start normally

**No manual SQL execution needed!**

### 3. Assign Admin Role to First User

After the app starts successfully, connect to the database and run:

```sql
-- Verify roles were created
SELECT * FROM AspNetRoles;

-- Assign Admin role to your user
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = 'YOUR_EMAIL@qualitybolt.com' AND r.Name = 'Admin';
```

### 4. Test the Admin Panel
1. Login to the application
2. You should see "User Management" in the navigation
3. Click it to access `/admin/users`
4. Create/edit/delete users as needed

## User Fields

When creating/editing users, you can set:
- ? Email
- ? Password
- ? Given Name
- ? Family Name
- ? **Ariba ID** (required)
- ? **Client ID** (optional, dropdown)
- ? **Roles** (multi-select: Admin, User)
- ? Disabled status

## Troubleshooting

### If migration fails on startup:
- Check application logs for migration errors
- Verify database connection string
- Ensure database server is accessible

### If roles aren't seeded:
- Check application logs
- Manually verify: `SELECT * FROM AspNetRoles`
- Roles should auto-create on first run

### If you can't access admin panel:
1. Go to `/debug/my-roles` to check your roles
2. Verify Admin role was assigned via SQL
3. Logout and login again to refresh claims

## Future Enhancements

When moving to true production:
- Disable automatic migrations: Remove `context.Database.Migrate()` from Program.cs
- Use manual SQL scripts generated via `dotnet ef migrations script`
- Implement proper change management process

## Files Modified/Created

### Created:
- `ShopQualityboltWeb\Controllers\Api\UsersController.cs`
- `QBExternalWebLibrary\Services\Http\UserApiService.cs`
- `ShopQualityboltWebBlazor\Components\Pages\AdminUsers.razor`
- `ShopQualityboltWebBlazor\Components\CustomComponents\UserDialog.razor`
- `ShopQualityboltWebBlazor\Components\Pages\DebugRoles.razor`
- `ShopQualityboltWebBlazor\Components\Pages\AccessDenied.razor`
- `QBExternalWebLibrary\Migrations\20251107144854_AddIdentityRoles.cs`

### Modified:
- `ShopQualityboltWeb\Program.cs` - Added automatic migration + role seeding
- `ShopQualityboltWebBlazor\Program.cs` - Added Client and User API services
- `ShopQualityboltWebBlazor\Components\Layout\NavMenu.razor` - Added admin section
- `QBExternalWebLibrary\Services\Http\ContentTypes\Identity\UserInfo.cs` - Added Roles property
- `ShopQualityboltWeb\Controllers\Api\AccountsController.cs` - Added roles to UserInfo response

---

? **Ready to deploy!** Just restart your application and the database will be automatically updated.
