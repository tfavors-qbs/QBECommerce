# Foreign Key Constraint Issue - ACTUAL ROOT CAUSE: Missing ContractItemId in Mapper

## The Problem

FK constraint violations were occurring when adding items to shopping carts, even though:
- ? Contract items existed in the database
- ? Validation was passing
- ? All checks confirmed the item was valid

### The Error
```
Microsoft.EntityFrameworkCore.DbUpdateException: 
'An error occurred while saving the entity changes. See the inner exception for details.' 
SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint 
"FK_ShoppingCartItems_ContractItems_ContractItemId". 
The conflict occurred in database "QBCommerceDB", table "dbo.ContractItems", column 'Id'.
```

### Example Case
- **Contract Item ID**: 1205 (K8Y M18 Finished Hex Nuts)
- **Validation**: Passes ?
- **Item Exists**: Confirmed in database ?
- **Insert Operation**: FK constraint violation ?

## The ACTUAL Root Cause

### The Bug Was in the Mapper

The `ShoppingCartItemMapper.MapToModel()` method was **NOT mapping the `ContractItemId` property** from the view model to the entity!

**File**: `QBExternalWebLibrary/Models/Mapping/ShoppingCartItemMapper.cs`

#### BEFORE (Broken):
```csharp
public ShoppingCartItem MapToModel(ShoppingCartItemEVM view) {
    var shoppingCartItem = _repository.GetById(view.Id);
    if (shoppingCartItem == null) {
        shoppingCartItem = new ShoppingCartItem {
            Id = view.Id,
            ShoppingCartId = view.ShoppingCartId,
            // ? ContractItemId is MISSING!
            Quantity = view.Quantity
        };
    } else {
        shoppingCartItem.ShoppingCartId = view.ShoppingCartId;
        // ? ContractItemId is MISSING!
        shoppingCartItem.Quantity = view.Quantity;
    }
    return shoppingCartItem;
}
```

### What Happened During Execution

1. Controller creates view model with valid data:
   ```csharp
   var newItem = new ShoppingCartItemEVM
   {
       ShoppingCartId = cart.Id,        // ? Set correctly
       ContractItemId = 1205,            // ? Set correctly  
       Quantity = request.Quantity       // ? Set correctly
   };
   ```

2. Service calls mapper:
   ```csharp
   _shoppingCartItemService.Create(null, newItem);
   // ? calls _mapper.MapToModel(newItem)
   ```

3. Mapper creates entity **WITHOUT ContractItemId**:
   ```csharp
   var entity = new ShoppingCartItem {
       ShoppingCartId = 123,  // ? Mapped
       ContractItemId = 0,    // ? DEFAULT VALUE (not mapped!)
       Quantity = 1           // ? Mapped
   };
   ```

4. Repository tries to insert with `ContractItemId = 0`:
   ```sql
   INSERT INTO ShoppingCartItems (ShoppingCartId, ContractItemId, Quantity)
   VALUES (123, 0, 1)  -- ? ContractItem with ID 0 doesn't exist!
   ```

5. SQL Server FK constraint violation:
   ```
   FK_ShoppingCartItems_ContractItems_ContractItemId constraint failed
   because ContractItem with Id = 0 does not exist
   ```

## The Fix

### Updated Mapper Code

Added `ContractItemId` to BOTH create and update paths in the mapper:

```csharp
public ShoppingCartItem MapToModel(ShoppingCartItemEVM view) {
    var shoppingCartItem = _repository.GetById(view.Id);
    if (shoppingCartItem == null) {
        shoppingCartItem = new ShoppingCartItem {
            Id = view.Id,
            ShoppingCartId = view.ShoppingCartId,
            ContractItemId = view.ContractItemId,  // ? FIXED: Now mapped
            Quantity = view.Quantity
        };
    } else {
        shoppingCartItem.ShoppingCartId = view.ShoppingCartId;
        shoppingCartItem.ContractItemId = view.ContractItemId;  // ? FIXED: Now mapped
        shoppingCartItem.Quantity = view.Quantity;
    }
    return shoppingCartItem;
}

public ShoppingCartItemEVM MapToEdit(ShoppingCartItem model) {
    return new ShoppingCartItemEVM {
        Id = model.Id,
        ShoppingCartId = model.ShoppingCartId,
        ContractItemId = model.ContractItemId,  // ? FIXED: For consistency
        Quantity = model.Quantity
    };
}
```

## Why This Was Hard to Diagnose

1. **Validation Worked**: Contract item 1205 existed and was found by all validation queries
2. **No Null Values**: The view model HAD the correct ContractItemId value
3. **Wrong ID**: The FK violation was for ID 0 (default int), not 1205
4. **Service Layer Abstraction**: The bug was hidden in the mapper, not the controller
5. **Silent Data Loss**: The mapper silently dropped the ContractItemId without error

## Verification

### Before Fix:
```csharp
var newItem = new ShoppingCartItemEVM {
    ContractItemId = 1205,  // Set in controller
    // ... other properties
};

// After mapper.MapToModel(newItem):
// entity.ContractItemId == 0  ? LOST!
```

### After Fix:
```csharp
var newItem = new ShoppingCartItemEVM {
    ContractItemId = 1205,  // Set in controller
    // ... other properties
};

// After mapper.MapToModel(newItem):
// entity.ContractItemId == 1205  ? PRESERVED!
```

## Testing the Fix

### Test 1: Add Valid Item
```
Given: Contract item 1205 exists
And: User has a cart
When: QBSales adds item 1205 to cart
Then: Item is added successfully ?
And: ShoppingCartItem.ContractItemId = 1205 ?
```

### Test 2: Verify Database
```sql
-- After successful add, verify the data
SELECT ShoppingCartId, ContractItemId, Quantity 
FROM ShoppingCartItems 
WHERE ContractItemId = 1205;

-- Should return:
-- ShoppingCartId | ContractItemId | Quantity
-- 123            | 1205          | 1
```

### Test 3: Add Multiple Items
```
Given: Multiple contract items exist
When: Add items 1205, 1139, etc.
Then: All items added successfully ?
And: Each has correct ContractItemId ?
```

## Impact of This Bug

### What Was Broken:
- ? Could NOT add ANY items to cart via service layer
- ? Every add operation failed with FK constraint violation
- ? Happened for ALL contract items (not just specific ones)
- ? Both QBSalesCartController and ShoppingCartsAPIController affected

### What Now Works:
- ? Can add items to cart
- ? ContractItemId properly stored
- ? FK constraints satisfied
- ? All cart operations functional

## Additional Safety Measures Added

While fixing the mapper was sufficient, I also added defensive programming in the controller:

```csharp
// Double-check validation before insert
var contractItemInContext = await _context.ContractItems
    .FirstOrDefaultAsync(ci => ci.Id == request.ContractItemId);
    
if (contractItemInContext == null)
{
    _logger.LogError("Contract item {ContractItemId} existed during validation but not during insert", 
        request.ContractItemId);
    return BadRequest($"Contract item is no longer available");
}

// Specific FK constraint exception handling
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_ShoppingCartItems_ContractItems") == true)
{
    _logger.LogError(ex, "FK constraint violation for contract item {ContractItemId}", 
        request.ContractItemId);
    return BadRequest($"Contract item is no longer available");
}
```

These additions provide:
- Race condition detection
- Better error messages
- Comprehensive logging

## Lesson Learned

**Always verify that mappers copy ALL required properties**, especially foreign key relationships. A missing property mapping can cause cryptic database constraint errors that appear unrelated to the actual bug.

## Files Changed

1. **QBExternalWebLibrary/Models/Mapping/ShoppingCartItemMapper.cs** ? PRIMARY FIX
   - Added `ContractItemId` to `MapToModel()` method (create path)
   - Added `ContractItemId` to `MapToModel()` method (update path)
   - Added `ContractItemId` to `MapToEdit()` method (for consistency)

2. **ShopQualityboltWeb/Controllers/Api/QBSalesCartController.cs** (Defense in depth)
   - Added double validation before insert
   - Added specific FK constraint exception handling
   - Enhanced logging

## Diagnostic Queries Used

```sql
-- Verified contract item exists
SELECT Id, CustomerStkNo, Description, ClientId 
FROM ContractItems 
WHERE Id = 1205;
-- Result: Found ?

-- Verified FK constraint definition
SELECT fk.name, OBJECT_NAME(fk.parent_object_id), 
       OBJECT_NAME(fk.referenced_object_id)
FROM sys.foreign_keys AS fk
WHERE fk.name = 'FK_ShoppingCartItems_ContractItems_ContractItemId';
-- Result: Constraint exists and is correct ?

-- The key insight was debugging the ACTUAL ContractItemId value
-- being inserted, which was 0 instead of 1205
```

## Success Criteria

? Mapper now copies ContractItemId from view to model  
? FK constraint violations resolved  
? All contract items can be added to carts  
? No more mysterious "item exists but can't insert" errors  
? Comprehensive logging for future issues  

---

**Status**: ? **FIXED**  
**Root Cause**: Missing property mapping in `ShoppingCartItemMapper`  
**Solution**: Add `ContractItemId` to mapper  
**Impact**: CRITICAL - Core cart functionality was broken  
**Bug Type**: Silent data loss in mapper layer  
**Detection Method**: Debugger inspection of entity before SaveChanges
