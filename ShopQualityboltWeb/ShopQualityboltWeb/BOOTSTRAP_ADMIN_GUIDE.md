# Bootstrap Admin Access - Quick Guide

## ?? TEMPORARY ENDPOINT - DELETE AFTER USE

This endpoint allows you to grant yourself Admin role without database access.

## How to Use

### Option 1: Using Postman/Insomnia/Thunder Client

**Endpoint:** `POST https://shop.qualitybolt.com/api/bootstrap/grant-admin`

**Headers:**
```
Content-Type: application/json
```

**Body (JSON):**
```json
{
  "email": "tfavors@qualitybolt.com",
  "secret": "TempAdminBootstrap2024!"
}
```

**Expected Response:**
```json
{
  "message": "Successfully granted Admin role to 'tfavors@qualitybolt.com'",
  "email": "tfavors@qualitybolt.com",
  "roles": ["Admin"]
}
```

### Option 2: Using PowerShell

```powershell
$body = @{
    email = "tfavors@qualitybolt.com"
    secret = "TempAdminBootstrap2024!"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://shop.qualitybolt.com/api/bootstrap/grant-admin" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

### Option 3: Using curl

```bash
curl -X POST https://shop.qualitybolt.com/api/bootstrap/grant-admin \
  -H "Content-Type: application/json" \
  -d '{
    "email": "tfavors@qualitybolt.com",
    "secret": "TempAdminBootstrap2024!"
  }'
```

### Option 4: Using JavaScript (Browser Console)

```javascript
fetch('https://shop.qualitybolt.com/api/bootstrap/grant-admin', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'tfavors@qualitybolt.com',
    secret: 'TempAdminBootstrap2024!'
  })
})
.then(response => response.json())
.then(data => console.log(data));
```

## List All Users (Optional)

To see all users and their roles:

**Endpoint:** `GET https://shop.qualitybolt.com/api/bootstrap/list-users?secret=TempAdminBootstrap2024!`

## After Granting Admin Access

1. **Login to the application**
2. **Verify you have admin access** - Go to `/debug/my-roles`
3. **Use the Admin panel** - Go to `/admin/users`
4. **DELETE THIS FILE** - `BootstrapAdminController.cs`
5. **Remove BootstrapSecret** from appsettings files

## Security Notes

- ? Endpoint is protected by a secret key
- ? Only grants roles to existing users
- ?? **DELETE THIS CONTROLLER** after granting admin to at least one user
- ?? Don't commit the secret to Git if using a public repository

## Troubleshooting

### Error: "User not found"
- Make sure the user account exists (register first)
- Check the email address is exactly correct

### Error: "Invalid bootstrap secret"
- Check the secret matches what's in appsettings.json
- Secret is case-sensitive

### Error: "Bootstrap is not configured"
- Make sure BootstrapSecret is in appsettings.json
- Restart the application after adding the setting

---

**Remember:** After you've granted yourself admin access and verified it works, **delete BootstrapAdminController.cs** for security!
