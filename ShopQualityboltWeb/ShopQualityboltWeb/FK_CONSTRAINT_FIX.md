# Foreign Key Constraint Violation Fix

## The Error
```
Error Number:547,State:0,Class:16
The INSERT statement conflicted with the FOREIGN KEY constraint
```

This error occurred when trying to add an item to a shopping cart.

## Root Cause
The `AddItemToUserCart` method was attempting to create a `ShoppingCartItem` with a `ContractItemId` that either:
1. Doesn't exist in the `ContractItems` table, OR
2. Belongs to a different client than the user

SQL Server's foreign key constraint prevented the invalid insert operation.

## The Problem Code
```csharp
// BEFORE: No validation
var newItem = new ShoppingCartItemEVM
{
    ShoppingCartId = cart.Id,
    ContractItemId = request.ContractItemId, // ? Might be invalid!
    Quantity = request.Quantity
};
_shoppingCartItemService.Create(null, newItem); // ? Throws FK constraint error
```

## The Solution
Added validation before attempting to create the shopping cart item:

```csharp
// AFTER: Validate before creating
// 1. Check if contract item exists
var contractItem = _contractItemService.GetById(request.ContractItemId);
if (contractItem == null)
    return BadRequest($"Contract item with ID {request.ContractItemId} not found");

// 2. Check if contract item belongs to user's client
if (user.ClientId.HasValue && contractItem.ClientId != user.ClientId.Value)
    return BadRequest($"Contract item does not belong to user's client");

// Now safe to create
var newItem = new ShoppingCartItemEVM { ... };
_shoppingCartItemService.Create(null, newItem); // ? No error
```

## Validation Steps
The fix adds two critical validation checks:

### 1. Contract Item Exists
```csharp
var contractItem = _contractItemService.GetById(request.ContractItemId);
if (contractItem == null)
    return BadRequest($"Contract item with ID {request.ContractItemId} not found");
```
**Prevents**: Attempting to add a non-existent item to the cart

### 2. Contract Item Belongs to User's Client
```csharp
if (user.ClientId.HasValue && contractItem.ClientId != user.ClientId.Value)
    return BadRequest($"Contract item does not belong to user's client");
```
**Prevents**: Adding items from one client to another client's user cart (security issue)

## HTTP Response Codes

| Scenario | Before | After |
|----------|--------|-------|
| Contract item doesn't exist | 500 Internal Server Error | 400 Bad Request |
| Contract item wrong client | 500 Internal Server Error | 400 Bad Request |
| Valid contract item | 200 OK | 200 OK |

## Security Improvement
This fix also adds a **security layer** by ensuring QBSales users cannot add items from Client A's contract to Client B's user cart, even if they know the contract item ID.

**Example Attack Scenario (Now Prevented):**
1. QBSales user looks up a contract item ID from "Acme Corp" (ID: 123)
2. Tries to add that item to a cart for "Widget Inc" user
3. **Before**: Would succeed if item existed, crossing client boundaries
4. **After**: Returns 400 Bad Request - "Contract item does not belong to user's client"

## Better Error Messages
Instead of a cryptic SQL error, users now get clear, actionable error messages:

### Before:
```
500 Internal Server Error
Error Number:547,State:0,Class:16
The INSERT statement conflicted with the FOREIGN KEY constraint...
```

### After:
```
400 Bad Request
Contract item with ID 999 not found
```

or

```
400 Bad Request
Contract item does not belong to user's client
```

## Enhanced Logging
Also improved logging to include more context:

```csharp
_logger.LogInformation("QBSales user {SalesUser} added item {ContractItemId} (Qty: {Quantity}) to cart for user {TargetUser}", 
    User.Identity?.Name, request.ContractItemId, request.Quantity, userId);
```

And on error:

```csharp
_logger.LogError(ex, "Error adding item to cart for user {UserId}. ContractItemId: {ContractItemId}", 
    userId, request.ContractItemId);
```

This helps with:
- Debugging issues
- Audit trail
- Security monitoring

## Files Changed
- `ShopQualityboltWeb/Controllers/Api/QBSalesCartController.cs`
  - Method: `AddItemToUserCart`
  - Added contract item existence validation
  - Added client ownership validation
  - Improved error messages
  - Enhanced logging

## Testing Scenarios

### Test 1: Valid Item Addition
1. QBSales user selects correct client
2. Adds item that belongs to that client
3. ? Should succeed with 200 OK
4. ? Item added to cart

### Test 2: Non-Existent Contract Item
1. QBSales user tries to add item ID 99999 (doesn't exist)
2. ? Should return 400 Bad Request
3. ? Error message: "Contract item with ID 99999 not found"

### Test 3: Wrong Client Item (Security)
1. QBSales user manages cart for "Acme Corp" user
2. Tries to add item from "Widget Inc" contract
3. ? Should return 400 Bad Request
4. ? Error message: "Contract item does not belong to user's client"

### Test 4: Update Existing Item
1. Item already in cart
2. Add same item again
3. ? Should update quantity instead of creating duplicate
4. ? Should succeed with 200 OK

## Database Constraints Respected
This fix respects the existing database schema:

```sql
-- ShoppingCartItems table has FK constraint
ALTER TABLE [ShoppingCartItems]
ADD CONSTRAINT [FK_ShoppingCartItems_ContractItems]
FOREIGN KEY ([ContractItemId])
REFERENCES [ContractItems]([Id]);
```

By validating before inserting, we:
- ? Prevent FK constraint violations
- ? Return meaningful errors
- ? Maintain data integrity
- ? Add security layer

## Best Practices Followed
1. **Validate input** before database operations
2. **Return specific HTTP status codes** (400 for bad request, not 500)
3. **Provide clear error messages** to help users/developers
4. **Log with context** for debugging and auditing
5. **Security by design** - validate client ownership
6. **Fail fast** - check early, don't waste resources

---

**Status**: ? **FIXED**  
**Error Type**: Foreign Key Constraint Violation (Error 547)  
**Impact**: High - Was preventing items from being added to carts  
**Solution**: Validate contract item exists and belongs to user's client  
**Security**: ? Improved - Prevents cross-client item addition
