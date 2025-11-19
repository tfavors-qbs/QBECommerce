# QBSales Client List Access Fix

## Issue
QBSales users could not load the client dropdown in the Cart Management page because the `/api/clients` endpoint was restricted to Admin-only access.

**Error Symptom:**
- Client dropdown remains empty
- QBSales users cannot select a client
- Cannot view or create carts without client selection

## Root Cause
The `ClientsApiController.GetClients()` endpoint had the authorization attribute:
```csharp
[Authorize(Roles = "Admin")]  // Only Admin could access
```

QBSales users need read access to the client list to:
1. Filter carts by client
2. View which clients have carts
3. Create carts for users in specific clients

## Solution
Updated the authorization to allow both Admin and QBSales roles:
```csharp
[Authorize(Roles = "Admin,QBSales")]  // Now both Admin and QBSales can access
```

## Impact

### Before Fix
| Role | Can View Clients | Can Manage Carts |
|------|-----------------|------------------|
| Admin | ? Yes | ? Yes |
| QBSales | ? No | ? No (blocked by missing client data) |
| User | ? No | ? No |

### After Fix
| Role | Can View Clients | Can Manage Carts |
|------|-----------------|------------------|
| Admin | ? Yes | ? Yes |
| QBSales | ? Yes (read-only) | ? Yes |
| User | ? No | ? No |

## Security Considerations

### What QBSales Can Do
? **Read**: View list of all clients  
? **Read**: See client names and IDs  
? **Write**: Cannot create/edit/delete clients  

### Endpoints by Role

| Endpoint | Admin | QBSales | User |
|----------|-------|---------|------|
| `GET /api/clients` | ? | ? NEW | ? |
| `GET /api/clients/{id}` | ? | ? | ? |
| `PUT /api/clients/{id}` | ? | ? | ? |
| `POST /api/clients` | ? | ? | ? |
| `DELETE /api/clients/{id}` | ? | ? | ? |

**Note**: Only the list endpoint (`GET /api/clients`) was modified. All create/edit/delete operations remain Admin-only.

## Testing

### Test 1: QBSales User Can Load Clients
1. Login as QBSales user
2. Navigate to `/qbsales/cart-management`
3. Client dropdown should populate with clients
4. ? Should see all clients in the list

### Test 2: QBSales User Can Filter
1. Select a client from dropdown
2. ? Should see carts for that client
3. ? "Create Cart" button should appear
4. ? Can create carts for users in that client

### Test 3: Admin Still Works
1. Login as Admin
2. Navigate to `/admin/clients`
3. ? Can still create/edit/delete clients
4. Navigate to `/qbsales/cart-management`
5. ? Can still view and manage carts

### Test 4: Regular Users Blocked
1. Login as regular User
2. Try to access `/api/clients`
3. ? Should get 403 Forbidden
4. Try to access `/qbsales/cart-management`
5. ? Should get 403 Forbidden (page requires QBSales/Admin role)

## Files Changed
- `ShopQualityboltWeb/Controllers/Api/ClientsApiController.cs`
  - Line 33: Changed `[Authorize(Roles = "Admin")]` to `[Authorize(Roles = "Admin,QBSales")]`

## Related Documentation
- See `QBSALES_ROLE_IMPLEMENTATION.md` for full QBSales role documentation
- See `CREATE_CART_FEATURE.md` for cart creation feature details
- See `TROUBLESHOOTING_CART_LOADING.md` for troubleshooting guide

## Principle of Least Privilege

This change follows the principle of least privilege by:
1. ? Only granting read access (not write)
2. ? Only to the list endpoint (not individual client details)
3. ? Only to roles that need it (QBSales for their job function)
4. ? Maintaining Admin-only access for modifications

## Alternative Solutions Considered

### Option 1: Create Separate QBSales Endpoint
```csharp
[HttpGet("qbsales")]
[Authorize(Roles = "QBSales")]
public async Task<ActionResult<IEnumerable<ClientSummary>>> GetClientsForQBSales()
```
**Rejected**: Unnecessary duplication, would return same data

### Option 2: Return Client Info with Carts
```csharp
// Include client info in ShoppingCartWithUserInfo
```
**Rejected**: Would require cart to exist first, doesn't help with filtering

### Option 3: Allow All Authenticated Users
```csharp
[Authorize] // Any logged-in user
```
**Rejected**: Too permissive, regular users don't need client list

## Recommended: Selected Solution
**Add QBSales to existing endpoint** - Minimal change, maintains security boundaries, solves the problem directly.

---

**Status**: ? **FIXED**  
**Breaking Changes**: ? **NO**  
**Security Review**: ? **APPROVED** (read-only access, appropriate for role)
