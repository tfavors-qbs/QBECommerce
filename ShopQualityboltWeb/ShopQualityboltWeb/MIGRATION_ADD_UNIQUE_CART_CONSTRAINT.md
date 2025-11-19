# Database Migration: Add Unique Constraint to Shopping Carts

## Overview
This migration adds a unique constraint on `ShoppingCart.ApplicationUserId` to ensure each user can only have ONE shopping cart.

## Steps to Apply Migration

### 1. Check for Existing Duplicate Carts

**Before applying the migration**, check if there are already duplicate carts in your database:

```sql
-- Check for users with multiple carts
SELECT 
    ApplicationUserId, 
    COUNT(*) as CartCount,
    STRING_AGG(CAST(Id AS VARCHAR), ', ') as CartIds
FROM ShoppingCarts
GROUP BY ApplicationUserId
HAVING COUNT(*) > 1
ORDER BY CartCount DESC;
```

If this query returns any results, you have duplicates that need to be resolved before applying the migration.

### 2. Resolve Duplicate Carts (If Any Exist)

#### Option A: Keep Cart with Most Items (Recommended)

```sql
-- For SQL Server
WITH RankedCarts AS (
    SELECT 
        sc.Id,
        sc.ApplicationUserId,
        COUNT(sci.Id) as ItemCount,
        ROW_NUMBER() OVER (
            PARTITION BY sc.ApplicationUserId 
            ORDER BY COUNT(sci.Id) DESC, sc.Id DESC
        ) as RowNum
    FROM ShoppingCarts sc
    LEFT JOIN ShoppingCartItems sci ON sc.Id = sci.ShoppingCartId
    GROUP BY sc.Id, sc.ApplicationUserId
)
DELETE FROM ShoppingCarts
WHERE Id IN (
    SELECT Id FROM RankedCarts WHERE RowNum > 1
);
```

#### Option B: Keep Newest Cart

```sql
-- For SQL Server
WITH RankedCarts AS (
    SELECT 
        Id,
        ApplicationUserId,
        ROW_NUMBER() OVER (
            PARTITION BY ApplicationUserId 
            ORDER BY Id DESC
        ) as RowNum
    FROM ShoppingCarts
)
DELETE FROM ShoppingCarts
WHERE Id IN (
    SELECT Id FROM RankedCarts WHERE RowNum > 1
);
```

#### Option C: Merge Carts (Manual Process)

If you want to preserve all items from all carts:

```sql
-- 1. Identify duplicate carts
SELECT 
    ApplicationUserId, 
    Id as CartId,
    (SELECT COUNT(*) FROM ShoppingCartItems WHERE ShoppingCartId = sc.Id) as ItemCount
FROM ShoppingCarts sc
WHERE ApplicationUserId IN (
    SELECT ApplicationUserId 
    FROM ShoppingCarts 
    GROUP BY ApplicationUserId 
    HAVING COUNT(*) > 1
)
ORDER BY ApplicationUserId, Id;

-- 2. For each user with duplicates, choose which cart to keep (e.g., Cart with lowest Id)

-- 3. Move all items from other carts to the kept cart
-- Example for user 'abc-123', keeping cart 10, merging from cart 20
UPDATE ShoppingCartItems 
SET ShoppingCartId = 10 
WHERE ShoppingCartId = 20;

-- 4. Delete the now-empty carts
DELETE FROM ShoppingCarts WHERE Id = 20;

-- 5. Repeat for each duplicate user
```

### 3. Create the Migration

Navigate to the API project directory:

```bash
cd ShopQualityboltWeb
```

Create the migration:

```bash
dotnet ef migrations add AddUniqueConstraintToShoppingCart --project ../QBExternalWebLibrary/QBExternalWebLibrary
```

This will generate a migration file like:
```
QBExternalWebLibrary/QBExternalWebLibrary/Migrations/YYYYMMDDHHMMSS_AddUniqueConstraintToShoppingCart.cs
```

### 4. Review the Generated Migration

The migration should look like this:

```csharp
public partial class AddUniqueConstraintToShoppingCart : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ShoppingCarts_ApplicationUserId_Unique",
            table: "ShoppingCarts",
            column: "ApplicationUserId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ShoppingCarts_ApplicationUserId_Unique",
            table: "ShoppingCarts");
    }
}
```

### 5. Apply the Migration

#### Development Environment

```bash
dotnet ef database update --project ../QBExternalWebLibrary/QBExternalWebLibrary
```

#### Production Environment

**Option A: Automatic Migration (Program.cs already configured)**

The migration will apply automatically on next application startup because `Program.cs` has:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = services.GetRequiredService<DataContext>();
    context.Database.Migrate(); // Applies pending migrations
}
```

**Option B: Manual Migration Script**

Generate a SQL script for DBA review:

```bash
dotnet ef migrations script --project ../QBExternalWebLibrary/QBExternalWebLibrary --output migration.sql
```

Then have your DBA execute the script.

### 6. Verify the Migration

After applying the migration:

```sql
-- Check that the unique index exists
SELECT 
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('ShoppingCarts')
AND i.name = 'IX_ShoppingCarts_ApplicationUserId_Unique';

-- Should return:
-- IndexName: IX_ShoppingCarts_ApplicationUserId_Unique
-- IsUnique: 1
-- ColumnName: ApplicationUserId
```

### 7. Test the Constraint

Try to create a duplicate cart (should fail):

```sql
-- Insert a test cart
INSERT INTO ShoppingCarts (ApplicationUserId) VALUES ('test-user-123');

-- Try to insert duplicate (should fail with unique constraint violation)
INSERT INTO ShoppingCarts (ApplicationUserId) VALUES ('test-user-123');
-- Error: Cannot insert duplicate key row in object 'dbo.ShoppingCarts' 
--        with unique index 'IX_ShoppingCarts_ApplicationUserId_Unique'

-- Clean up test data
DELETE FROM ShoppingCarts WHERE ApplicationUserId = 'test-user-123';
```

---

## Rollback Plan

If you need to rollback the migration:

### Remove Migration (Before Applying to Database)

```bash
dotnet ef migrations remove --project ../QBExternalWebLibrary/QBExternalWebLibrary
```

### Rollback Applied Migration

```bash
# Rollback to previous migration
dotnet ef database update <PreviousMigrationName> --project ../QBExternalWebLibrary/QBExternalWebLibrary

# Or rollback all migrations
dotnet ef database update 0 --project ../QBExternalWebLibrary/QBExternalWebLibrary
```

### Manual SQL Rollback

```sql
-- Remove the unique index
DROP INDEX IX_ShoppingCarts_ApplicationUserId_Unique ON ShoppingCarts;

-- Remove migration history entry
DELETE FROM __EFMigrationsHistory 
WHERE MigrationId = 'YYYYMMDDHHMMSS_AddUniqueConstraintToShoppingCart';
```

---

## Post-Migration Code Changes

After the migration is applied, update your code to handle the unique constraint properly:

### Update ShoppingCartsAPIController

Add better error handling:

```csharp
[HttpGet()]
[Authorize]
public async Task<ActionResult<ShoppingCartEVM>> GetShoppingCart() 
{
    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
    
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null) return NotFound(new { message = "User not found." });
    
    // Now safe to use FirstOrDefault - unique constraint ensures only 1 cart
    var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
    
    if (usersShoppingCart == null) 
    {
        try 
        {
            ShoppingCartEVM cart = new ShoppingCartEVM() { ApplicationUserId = user.Id };
            _service.Create(null, cart);
            return CreatedAtAction("GetShoppingCart", new { id = cart.Id }, cart);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ShoppingCarts_ApplicationUserId_Unique") == true)
        {
            // Another request created the cart concurrently
            _logger.LogWarning("Concurrent cart creation detected for user {UserId}", userId);
            usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
            if (usersShoppingCart == null)
                return StatusCode(500, "Failed to create or retrieve shopping cart");
        }
    }
    
    return _mapper.MapToEdit(usersShoppingCart);
}
```

---

## Monitoring & Validation

### Add Application Logging

```csharp
// In your service layer or repository
public ShoppingCart CreateCart(string userId)
{
    var existingCarts = _context.ShoppingCarts
        .Where(sc => sc.ApplicationUserId == userId)
        .ToList();
    
    if (existingCarts.Count > 0)
    {
        _logger.LogWarning(
            "Attempted to create cart for user {UserId} who already has {CartCount} cart(s). Returning existing cart.",
            userId, existingCarts.Count);
        return existingCarts.First();
    }
    
    try
    {
        var newCart = new ShoppingCart { ApplicationUserId = userId };
        _context.ShoppingCarts.Add(newCart);
        _context.SaveChanges();
        _logger.LogInformation("Created shopping cart {CartId} for user {UserId}", newCart.Id, userId);
        return newCart;
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_ShoppingCarts_ApplicationUserId_Unique") == true)
    {
        _logger.LogError(ex, "Unique constraint violation creating cart for user {UserId}", userId);
        
        // Reload from database
        return _context.ShoppingCarts.FirstOrDefault(sc => sc.ApplicationUserId == userId);
    }
}
```

### Create Monitoring Query

```sql
-- Daily check for any anomalies
SELECT 
    'Total Carts' as Metric,
    COUNT(*) as Value
FROM ShoppingCarts

UNION ALL

SELECT 
    'Users with Carts' as Metric,
    COUNT(DISTINCT ApplicationUserId) as Value
FROM ShoppingCarts

UNION ALL

SELECT 
    'Empty Carts' as Metric,
    COUNT(*) as Value
FROM ShoppingCarts sc
LEFT JOIN ShoppingCartItems sci ON sc.Id = sci.ShoppingCartId
WHERE sci.Id IS NULL;
```

---

## FAQ

### Q: What happens if a user already has 2 carts when I apply the migration?
**A:** The migration will **FAIL** with a unique constraint violation error. You MUST clean up duplicates first (see Step 2 above).

### Q: Will this affect existing users?
**A:** No, as long as they only have 1 cart each. Users with multiple carts must be cleaned up first.

### Q: What if QBSales creates a cart and then the user tries to create one?
**A:** The unique constraint will prevent the second cart creation. Your code should catch the exception and use the existing cart.

### Q: Can I rollback this migration easily?
**A:** Yes, see the Rollback Plan section above. But once rolled back, you're back to the "multiple carts" problem.

### Q: How do I test this in my dev environment?
**A:** 
1. Apply the migration
2. Try to insert a duplicate cart via SQL (should fail)
3. Try to create a duplicate cart via the API (should be handled gracefully)
4. Verify all cart operations still work normally

---

## Success Criteria

? Migration applied successfully  
? Unique index exists in database  
? No duplicate carts exist  
? Attempt to create duplicate cart fails gracefully  
? All cart operations (add, update, delete, checkout) work correctly  
? QBSales cart management works correctly  
? Application logs show no unique constraint violations  

---

**Status**: ? **READY TO APPLY**  
**Risk Level**: ?? **MEDIUM** (requires duplicate cleanup if any exist)  
**Downtime Required**: ? **NONE** (can apply while running)  
**Reversible**: ? **YES** (can rollback if needed)
