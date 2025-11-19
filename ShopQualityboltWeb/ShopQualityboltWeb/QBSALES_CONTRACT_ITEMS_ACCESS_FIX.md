# QBSales Contract Items Access Fix

## The Problem
QBSales users couldn't populate the list of contract items when managing a cart because:
1. The existing `GET /api/contractitems` endpoint only returned items for the authenticated user's own client
2. The `GET /api/contractitems/admin/client/{clientId}` endpoint required Admin role
3. QBSales users need to see contract items for ANY client they're managing

## Root Cause

### API Endpoint Authorization
The `ContractItemsApiController` had two endpoints for getting contract items:

1. **`GET /api/contractitems`** - Only returns items for the authenticated user's client
   ```csharp
   [HttpGet]
   [Authorize] // Any authenticated user
   public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItems() {
       // Returns items only for user.ClientId
       return _mapper.MapToEdit(_service.GetAll().Where(x => x.ClientId == user.ClientId));
   }
   ```

2. **`GET /api/contractitems/admin/client/{clientId}`** - Admin only
   ```csharp
   [HttpGet("admin/client/{clientId}")]
   [Authorize(Roles = "Admin")] // Admin role required!
   public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItemsByClient(int clientId)
   ```

### Frontend Usage
The `CartEditorDialog` was trying to use the admin endpoint:
```csharp
string endpoint = ClientId.HasValue 
    ? $"api/contractitems/admin/client/{ClientId.Value}"  // ? Requires Admin!
    : "api/contractitems/admin";
```

**Result**: QBSales users got 403 Forbidden when trying to load contract items.

## The Solution

### 1. Added QBSales-Friendly Endpoint
Created a new endpoint that allows both Admin and QBSales roles:

```csharp
[HttpGet("client/{clientId}")]
[Authorize(Roles = "Admin,QBSales")]  // ? Both roles allowed!
public async Task<ActionResult<IEnumerable<ContractItemEditViewModel>>> GetContractItemsByClientForQBSales(int clientId) {
    var contractItems = _service.GetAll().Where(x => x.ClientId == clientId);
    return _mapper.MapToEdit(contractItems);
}
```

### 2. Updated Frontend to Use New Endpoint
Modified `CartEditorDialog.razor` to use the QBSales-friendly endpoint:

```csharp
// BEFORE:
var response = await _httpClient.GetAsync($"api/contractitems/admin/client/{ClientId.Value}");

// AFTER:
var response = await _httpClient.GetAsync($"api/contractitems/client/{ClientId.Value}");
```

## API Endpoints Summary

| Endpoint | Authorization | Purpose |
|----------|--------------|---------|
| `GET /api/contractitems` | Any authenticated user | Get items for user's own client |
| `GET /api/contractitems/client/{clientId}` | **Admin, QBSales** ?NEW | Get items for any client (QBSales use) |
| `GET /api/contractitems/admin/client/{clientId}` | Admin only | Get items for any client (Admin use) |
| `GET /api/contractitems/{id}` | Authenticated (with client check) | Get single item |
| `PUT /api/contractitems/{id}` | Admin only | Update item |
| `POST /api/contractitems` | Admin only | Create item |
| `DELETE /api/contractitems/{id}` | Admin only | Delete item |

## Access Control Matrix

| Role | Own Client Items | Other Client Items | Create/Edit/Delete |
|------|-----------------|-------------------|-------------------|
| User | ? Read | ? No Access | ? No Access |
| QBSales | ? Read | ? **Read** (NEW!) | ? No Access |
| Admin | ? Read | ? Read | ? Full Access |

## Security Considerations

### ? Safe to Allow
The new endpoint is **read-only** and only returns contract item data:
- QBSales can view contract items to add them to carts
- QBSales **cannot** create, edit, or delete contract items
- QBSales **cannot** modify pricing or other sensitive data
- Data is filtered by `clientId` - no cross-client data exposure

### Why This is Necessary
QBSales users need to:
1. Manage carts for users in different clients
2. See available contract items for each client
3. Add appropriate items to each client's user carts

Without this access, QBSales users couldn't perform their job function.

## Files Changed

### Backend API
**`ShopQualityboltWeb/Controllers/Api/ContractItemsApiController.cs`**
- Added new method: `GetContractItemsByClientForQBSales(int clientId)`
- Route: `GET /api/contractitems/client/{clientId}`
- Authorization: `[Authorize(Roles = "Admin,QBSales")]`

### Frontend Blazor
**`ShopQualityboltWebBlazor/Components/CustomComponents/CartEditorDialog.razor`**
- Updated `LoadAvailableItems()` method
- Changed from: `api/contractitems/admin/client/{ClientId}`
- Changed to: `api/contractitems/client/{ClientId}`
- Added better error handling and logging

## Testing Scenarios

### Test 1: QBSales Load Contract Items
1. Login as QBSales user
2. Navigate to Cart Management
3. Select a client
4. Click "Manage" on a cart
5. ? Should load contract items in the autocomplete
6. ? Should be able to search and select items

### Test 2: QBSales Add Items to Cart
1. After loading cart editor (Test 1)
2. Search for a contract item
3. Select item and set quantity
4. Click "Add to Cart"
5. ? Should successfully add item
6. ? Item should appear in cart list

### Test 3: Regular User Access (Security)
1. Login as regular User (not QBSales)
2. Try to access: `GET /api/contractitems/client/5`
3. ? Should return 403 Forbidden
4. ? Security boundary maintained

### Test 4: Admin Access
1. Login as Admin
2. Access both endpoints:
   - `GET /api/contractitems/client/5`
   - `GET /api/contractitems/admin/client/5`
3. ? Both should work
4. ? Should return same data

## Error Messages Improved

### Before:
```
Failed to load items
```

### After:
```
Failed to load items: Forbidden
Cannot load items: Client ID not provided
Error loading items: [specific error message]
```

## Related Features

This fix complements:
- ? QBSales cart creation feature
- ? QBSales cart management feature
- ? Client filtering functionality
- ? Foreign key constraint validation

## Best Practices Followed

1. **Principle of Least Privilege**: Only granted read access, not write
2. **Role-Based Access Control**: Explicit roles required
3. **Endpoint Separation**: Kept admin endpoint separate
4. **Clear Naming**: New endpoint clearly indicates purpose
5. **Error Handling**: Improved error messages for debugging
6. **Security First**: Maintained proper authorization checks

## Deployment Notes

? **No Breaking Changes**: Existing endpoints unchanged  
? **Backward Compatible**: Admin users can use either endpoint  
? **Database**: No schema changes required  
? **Configuration**: No config changes required  

---

**Status**: ? **FIXED**  
**Issue**: QBSales users couldn't load contract items  
**Solution**: Added QBSales-friendly read-only endpoint  
**Security**: ? Read-only access, properly scoped  
**Testing**: ? Ready for testing
