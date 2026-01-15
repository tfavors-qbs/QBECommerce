# Past Orders Feature Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow users to view their organization's past orders, with filtering, tagging, and reorder capabilities.

**Architecture:** Follows existing QuickOrder patterns for data model, visibility, and UI. Integrates with checkout flow to capture orders before PunchOut submission.

**Tech Stack:** ASP.NET Core API, Entity Framework Core, Blazor (MudBlazor components)

---

## 1. Data Model

### PastOrder Entity
```csharp
public class PastOrder
{
    public int Id { get; set; }

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [ForeignKey("Client")]
    public int? ClientId { get; set; }  // Captured at order time
    public Client? Client { get; set; }

    public string? PONumber { get; set; }
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<PastOrderItem>? Items { get; set; }
    public List<PastOrderTag>? Tags { get; set; }
}
```

### PastOrderItem Entity
```csharp
public class PastOrderItem
{
    public int Id { get; set; }

    [ForeignKey("PastOrder")]
    public int PastOrderId { get; set; }
    public PastOrder PastOrder { get; set; } = null!;

    [ForeignKey("ContractItem")]
    public int ContractItemId { get; set; }
    public ContractItem ContractItem { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // Price at time of order
}
```

### PastOrderTag Entity
```csharp
public class PastOrderTag
{
    public int Id { get; set; }

    [ForeignKey("PastOrder")]
    public int PastOrderId { get; set; }
    public PastOrder PastOrder { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
}
```

### Key Design Decisions
- **No IsSharedClientWide flag** - All orders are automatically shared within organization
- **ClientId captured at order time** - Not derived from user's current client
- **UnitPrice snapshot** - Preserves price at time of order for historical accuracy
- **Reference ContractItemId** - ContractItems are source of truth for product details

---

## 2. Visibility Rules

**My Orders tab:**
```
UserId = currentUser.Id AND ClientId = currentUser.ClientId
```

**Organization Orders tab:**
```
ClientId = currentUser.ClientId AND UserId != currentUser.Id
```

**Key behavior:** If a user leaves an organization (ClientId changes), they lose access to ALL orders from that organization - including their own past orders.

---

## 3. Cart Page Changes

### New UI Elements (inline, above checkout button)
- **PO Number field** - Text input, optional, placeholder "Enter PO Number (optional)"
- **Tags field** - Chip input (freeform), optional, same component as QuickOrder editor

### Modified Checkout Flow
1. User fills cart as normal
2. Optionally enters PO number and tags
3. Clicks Checkout
4. **NEW:** Create PastOrder record with:
   - `UserId` = current user
   - `ClientId` = current user's ClientId (snapshot)
   - `PONumber` = form value
   - `TotalAmount` = cart total
   - `ItemCount` = cart item count
   - `Items` = cart items with current unit prices
   - `Tags` = form tags
5. Continue with existing PunchOut flow (generate cXML, submit to Ariba)
6. Clear cart (existing behavior)

---

## 4. Past Orders Page

### Route
`/past-orders`

### Layout (mirrors Quick Orders page)
- **Left sidebar:** Search box, sort dropdown, tag filter chips
- **Right content:** Tabs with order cards

### Tabs
- **My Orders** - Orders placed by current user (with ClientId match)
- **Organization Orders** - Orders placed by others in same organization

### Sort Options
- Date (newest first) - default
- Date (oldest first)
- Total amount (high to low)
- Total amount (low to high)

### Order Card Display
- PO Number (or "No PO" if empty)
- Order date (formatted: "Jan 15, 2026")
- Total amount (currency formatted)
- Item count ("5 items")
- Tags as chips
- Who placed it (Organization tab only - name or email)

### Card Actions
- **View** button opens detail dialog

---

## 5. Order Detail Dialog

### Header Section
- PO Number (prominent) or "No PO Number"
- Order date
- Total amount
- Placed by (name/email)

### Tags Section
- Editable chip input
- Changes saved on dialog close or explicit save

### Line Items Table
| Product | Description | Qty | Unit Price | Line Total |
|---------|-------------|-----|------------|------------|

- If ContractItem no longer exists, show "Product unavailable" in red

### Action Buttons
- **Reorder** - Shows confirmation dialog listing available vs unavailable items, then adds available items to cart
- **Convert to Quick Order** - Opens Quick Order creation dialog pre-populated with items and tags
- **Close**

---

## 6. Reorder Flow

When user clicks Reorder:

1. Check each PastOrderItem's ContractItem availability
2. Show confirmation dialog:
   - "Available items (will be added to cart):" - list with quantities
   - "Unavailable items (will be skipped):" - list with quantities (if any)
   - "Add to Cart" / "Cancel" buttons
3. If user confirms, add available items to cart
4. Show snackbar: "Added X items to cart" (or "Added X items, Y unavailable items skipped")

---

## 7. Convert to Quick Order Flow

When user clicks Convert to Quick Order:

1. Open QuickOrderEditorDialog with:
   - `IsNew = true`
   - Pre-populated items from PastOrder (only available ContractItems)
   - Pre-populated tags from PastOrder
   - Name field empty (user must provide)
2. User completes Quick Order creation as normal
3. Show snackbar on success: "Quick Order created"

---

## 8. API Endpoints

### Controller: `PastOrdersApiController`
**Route:** `api/past-orders`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/` | Get all past orders for current user's client (both tabs data) |
| GET | `/{id}` | Get single order with full item details |
| PUT | `/{id}/tags` | Update tags on an order |
| POST | `/{id}/reorder` | Check availability, add items to cart, return result |
| POST | `/{id}/convert-to-quickorder` | Create QuickOrder from PastOrder |

### Response Models

**PastOrderPageEVM:**
```csharp
public class PastOrderPageEVM
{
    public List<PastOrderEVM> MyOrders { get; set; }
    public List<PastOrderEVM> OrganizationOrders { get; set; }
    public List<string> AllTags { get; set; }
}
```

**PastOrderEVM:**
```csharp
public class PastOrderEVM
{
    public int Id { get; set; }
    public string? PONumber { get; set; }
    public DateTime OrderedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<string> Tags { get; set; }
    public string UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}
```

**PastOrderDetailEVM:**
```csharp
public class PastOrderDetailEVM : PastOrderEVM
{
    public List<PastOrderItemEVM> Items { get; set; }
}
```

**PastOrderItemEVM:**
```csharp
public class PastOrderItemEVM
{
    public int Id { get; set; }
    public int ContractItemId { get; set; }
    public string ProductName { get; set; }
    public string Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsAvailable { get; set; }
}
```

**ReorderResultEVM:**
```csharp
public class ReorderResultEVM
{
    public List<PastOrderItemEVM> AddedItems { get; set; }
    public List<PastOrderItemEVM> SkippedItems { get; set; }
    public string Message { get; set; }
}
```

---

## 9. Service Layer

### PastOrderService
- `CreateFromCartAsync(cart, poNumber, tags)` - Called during checkout
- `GetPageAsync(userId, clientId)` - Returns PastOrderPageEVM
- `GetByIdAsync(id, clientId)` - Returns PastOrderDetailEVM (with visibility check)
- `UpdateTagsAsync(id, tags, clientId)` - Update tags (with visibility check)
- `ReorderAsync(id, clientId, cartService)` - Add items to cart
- `ConvertToQuickOrderAsync(id, clientId, quickOrderService)` - Create QuickOrder

### PastOrderApiService (Blazor HTTP client)
- Mirrors controller endpoints
- Located in `QBExternalWebLibrary/Services/Http/`

---

## 10. Database Changes

### New DbSets in DataContext
```csharp
public DbSet<PastOrder> PastOrders { get; set; }
public DbSet<PastOrderItem> PastOrderItems { get; set; }
public DbSet<PastOrderTag> PastOrderTags { get; set; }
```

### Migration
- Create PastOrders table with foreign keys to ApplicationUser and Client
- Create PastOrderItems table with foreign keys to PastOrder and ContractItem
- Create PastOrderTags table with foreign key to PastOrder
- Index on PastOrders.ClientId for efficient organization queries
- Index on PastOrders.UserId for efficient user queries

---

## 11. Navigation

Add "Past Orders" to main navigation menu:
- Icon: `Icons.Material.Filled.History` or `Icons.Material.Filled.Receipt`
- Position: After "Quick Orders" in the menu
- Route: `/past-orders`

---

## 12. Error Handling

Follow existing IErrorLogService pattern:
- Log errors in all API endpoints
- Include userId, clientId, orderId in error context
- Return appropriate HTTP status codes (404, 403, 500)

---

## Files to Create/Modify

### New Files
- `QBExternalWebLibrary/Models/Catalog/PastOrder.cs`
- `QBExternalWebLibrary/Models/Catalog/PastOrderItem.cs`
- `QBExternalWebLibrary/Models/Catalog/PastOrderTag.cs`
- `QBExternalWebLibrary/Models/Pages/PastOrderPageEVM.cs`
- `QBExternalWebLibrary/Models/Catalog/PastOrderEVM.cs`
- `QBExternalWebLibrary/Models/Catalog/PastOrderItemEVM.cs`
- `QBExternalWebLibrary/Models/Mapping/PastOrderMapper.cs`
- `QBExternalWebLibrary/Services/Http/PastOrderApiService.cs`
- `QBExternalWebLibrary/Data/Repositories/PastOrderRepository.cs`
- `ShopQualityboltWeb/Controllers/Api/PastOrdersApiController.cs`
- `ShopQualityboltWeb/Services/PastOrderService.cs`
- `ShopQualityboltWebBlazor/Components/Pages/PastOrders.razor`
- `ShopQualityboltWebBlazor/Components/CustomComponents/PastOrderDetailDialog.razor`

### Modified Files
- `QBExternalWebLibrary/Data/DataContext.cs` - Add DbSets
- `ShopQualityboltWebBlazor/Components/Pages/Cart.razor` - Add PO/tags fields, create PastOrder at checkout
- `ShopQualityboltWebBlazor/Components/Layout/NavMenu.razor` - Add Past Orders nav item
- `ShopQualityboltWeb/Program.cs` - Register new services
- `ShopQualityboltWebBlazor/Program.cs` - Register PastOrderApiService
