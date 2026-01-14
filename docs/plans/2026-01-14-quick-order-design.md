# Quick Order Feature Design

## Overview

Quick Orders allow users to save cart configurations for reuse. Users can save their current cart as a Quick Order, manage multiple Quick Orders, and add items from any Quick Order back to their cart with a single action. QBSales users can create Quick Orders on behalf of customers.

## Key Decisions

| Decision | Choice |
|----------|--------|
| Feature name | Quick Order |
| Scope | User-specific by default, can be shared client-wide |
| Ownership | Creator is owner; QBSales-created ones transfer to target user |
| Editing | Full editing (items, quantities, name, tags, visibility) |
| Discontinued items | Show as unavailable (grayed out, not removed) |
| Adding to cart | Add quantities to existing items |
| UI location | Dedicated page in main navigation |
| Save entry point | "Save as Quick Order" button on Cart page |
| Copying | Users can copy shared Quick Orders they don't own |
| Limits | No limit on number of Quick Orders |
| QBSales workflow | Dedicated page (separate from Cart Management) |
| Organization | Sorting + free-form tags with autocomplete |
| Deletion | Soft delete with QBSales recovery |
| Analytics | Basic usage stats |

---

## Data Model

### QuickOrder Table

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| Name | string | User-provided name |
| OwnerId | string (FK) | ApplicationUser who owns this |
| IsSharedClientWide | bool | Whether visible to all client users |
| CreatedAt | datetime | When created |
| LastUsedAt | datetime? | Last time items were added to cart |
| TimesUsed | int | Count of times used (for analytics) |
| IsDeleted | bool | Soft delete flag (default false) |
| DeletedAt | datetime? | When soft-deleted |

### QuickOrderItem Table

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| QuickOrderId | int (FK) | Parent Quick Order |
| ContractItemId | int (FK) | The contract item |
| Quantity | int | Desired quantity |

**Note:** No price field. Prices are always pulled fresh from ContractItem via the CustomerStkNo reference.

### QuickOrderTag Table

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| QuickOrderId | int (FK) | Parent Quick Order |
| Tag | string | Tag text |

---

## User Features

### Quick Orders Page

**Location:** "Quick Orders" link in main navigation

**Page Elements:**
- Header: "My Quick Orders"
- Search box (searches name and tags)
- Sort dropdown: Name (A-Z), Date Created, Last Used, Most Used
- Tag filter (click tags to filter)
- "Create Quick Order" button

**Quick Order List:**
Each item displays:
- Name
- Tags (as chips/badges)
- Item count and total value (current prices)
- Visibility indicator (icon if shared client-wide)
- Last used date
- Actions: Edit, Copy, Delete

**Shared With Me Section:**
- Client-wide Quick Orders owned by others
- Shows owner name
- Actions: Use, Copy (no edit/delete)

### Quick Order Detail/Edit View

**Header Section:**
- Editable name field
- Tags input (free-form with autocomplete)
- Toggle: "Share with my organization"
- Created date, last used date (read-only)

**Items Section:**
- Table with columns:
  - Customer Stock Number
  - Description
  - Current Price
  - Quantity (editable)
  - Line total
  - Remove button
- Unavailable items: grayed out with "No longer available"
- "Add Items" button (opens product search)

**Footer Section:**
- Total items count
- Total value (excluding unavailable)
- "Add All to Cart" button
- "Add Selected to Cart" button (checkbox mode)
- Save / Cancel

### Cart Page Integration

**New Elements:**
- "Save as Quick Order" button (alongside Check Out, Clear Cart)

**Save Dialog:**
- Name field (required)
- Tags field (optional, with autocomplete)
- "Share with organization" checkbox (default unchecked)
- Save / Cancel buttons

**Behavior:**
- Creates Quick Order with current cart items
- Cart remains unchanged
- Success message with link to view Quick Order

---

## QBSales Features

### Quick Order Management Page

**Location:** "Quick Orders" in QBSales navigation (separate from Cart Management)

**Page Layout:**

**User Selection:**
- Client filter dropdown
- User search within client
- User list shows Quick Order count per user

**Selected User View:**
- All Quick Orders for that user
- "Create Quick Order for [User Name]" button
- "Show Deleted" toggle for recovery

**Creating Quick Order:**
- Same editor as user-facing
- Selects items from user's client catalog
- User becomes owner automatically
- Can set name, tags, visibility

**Permissions:**
- View user's Quick Orders (read-only)
- Create new Quick Orders (user becomes owner)
- Restore soft-deleted Quick Orders
- Cannot edit or delete user's existing Quick Orders

---

## Edge Cases

### Unavailable Items

- On load, check each ContractItemId exists for user's client
- Missing items marked as unavailable (not removed)
- "Add to Cart" skips unavailable items
- Message: "Added 8 items (2 unavailable items skipped)"

### Adding to Cart

- Check if ContractItem already in cart
- If yes: add quantities together
- If no: create new cart item
- Always use current ContractItem.Price

### Copying Quick Orders

- Creates new Quick Order with same items/quantities
- Copier becomes owner
- Name: "[Original Name] (Copy)"
- Tags copied
- IsSharedClientWide defaults to false

### Soft Delete

**User deletes:**
- Sets IsDeleted = true, DeletedAt = now
- Disappears from user's view
- Confirmation: "Quick Order '[name]' deleted"

**QBSales recovery:**
- "Show Deleted" toggle reveals deleted Quick Orders
- "Restore" action: IsDeleted = false, DeletedAt = null

**Shared Quick Orders:**
- If owner deletes, disappears for everyone
- QBSales can restore for all

### Security

- Users see: own Quick Orders + client-wide from same client
- Users edit/delete: only owned Quick Orders
- QBSales: only access users in their clients
- API validates ownership and client membership

---

## Analytics

### Metrics Tracked

**Overall:**
- Total Quick Orders created (all time, last 30 days)
- Total times used to add to cart
- Active users (users with at least one Quick Order)

**Breakdowns:**
- By client: adoption per client
- Most used Quick Orders (top 10/20)
- Shared vs private ratio

### Data Points

- QuickOrder.CreatedAt - when created
- QuickOrder.LastUsedAt - updated on "Add to Cart"
- QuickOrder.TimesUsed - incremented on "Add to Cart"

### Display

- Dashboard with key metric cards
- Optional: trend chart for adoption over time

---

## API Endpoints (Proposed)

### User Endpoints

```
GET    /api/quickorders                    - List user's Quick Orders + shared
GET    /api/quickorders/{id}               - Get Quick Order details
POST   /api/quickorders                    - Create Quick Order
PUT    /api/quickorders/{id}               - Update Quick Order
DELETE /api/quickorders/{id}               - Soft delete Quick Order
POST   /api/quickorders/{id}/copy          - Copy Quick Order
POST   /api/quickorders/{id}/add-to-cart   - Add items to cart
GET    /api/quickorders/tags               - Get user's used tags (for autocomplete)
```

### QBSales Endpoints

```
GET    /api/qbsales/quickorders/user/{userId}              - List user's Quick Orders
GET    /api/qbsales/quickorders/user/{userId}/deleted      - List deleted Quick Orders
POST   /api/qbsales/quickorders/user/{userId}              - Create Quick Order for user
POST   /api/qbsales/quickorders/{id}/restore               - Restore deleted Quick Order
```

### Analytics Endpoints

```
GET    /api/admin/quickorders/analytics    - Get usage statistics
```

---

## UI Components (Proposed)

### New Pages

- `QuickOrders.razor` - User's Quick Order list
- `QuickOrderEditor.razor` - Detail/edit view (or dialog)
- `QBSales/QuickOrderManagement.razor` - QBSales management page
- `Admin/QuickOrderAnalytics.razor` - Analytics dashboard

### New Dialogs

- `SaveAsQuickOrderDialog.razor` - Cart page save dialog
- `AddItemsDialog.razor` - Product search for adding items

### Modified Pages

- `Cart.razor` - Add "Save as Quick Order" button

---

## Implementation Notes

- No price storage in Quick Orders; always reference ContractItem
- Reuse existing product search/catalog components where possible
- Follow existing patterns for API controllers and services
- Use MudBlazor components consistent with rest of application
