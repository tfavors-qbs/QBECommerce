# Create Shopping Cart Feature for QBSales

## Overview
Added functionality to allow QBSales users to create shopping carts for users who don't have one yet, making it easier to prepare carts before punchout sessions.

## Changes Made

### Backend API (ShopQualityboltWeb)

#### QBSalesCartController.cs - New Endpoints

1. **GET `/api/qbsales/carts/client/{clientId}/users`**
   - Returns all users for a specific client with cart status
   - Shows which users have carts and which don't
   - Response: `List<UserCartInfo>`

2. **POST `/api/qbsales/carts/user/{userId}/create`**
   - Creates a new empty shopping cart for a specific user
   - Checks if cart already exists (returns Conflict if it does)
   - Logs creation action for audit trail
   - Response: `ShoppingCartPageEVM`

#### New DTO Classes

```csharp
public class UserCartInfo
{
    public string UserId { get; set; }
    public string UserEmail { get; set; }
    public string UserName { get; set; }
    public bool HasCart { get; set; }
    public int? CartId { get; set; }
}
```

### Frontend UI (ShopQualityboltWebBlazor)

#### CartManagement.razor - Updated

**New UI Elements:**
- **"Create Cart" button** - Appears when a client is selected
- Positioned next to the Refresh button in the Active Shopping Carts section

**New Methods:**
- `OpenCreateCartDialog()` - Loads users without carts and shows selection dialog
- `CreateCart(userId)` - Calls API to create cart for selected user
- Automatically refreshes cart list after creation

#### CreateCartDialog.razor - New Component

**Purpose:** Dialog for selecting which user to create a cart for

**Features:**
- Shows list of users from selected client who don't have carts
- Displays user name and email for easy identification
- Clickable list items for selection
- Disabled "Create Cart" button until user is selected
- Proper validation and error handling

**Layout:**
```
???????????????????????????????????????
? Create Shopping Cart                ?
???????????????????????????????????????
? Select a user from [Client] to     ?
? create a shopping cart for:        ?
?                                     ?
? ????????????????????????????????????
? ? ? John Doe                      ??
? ?   john.doe@client.com           ??
? ????????????????????????????????????
? ?   Jane Smith                    ??
? ?   jane.smith@client.com         ??
? ????????????????????????????????????
?                                     ?
?         [Cancel] [Create Cart]     ?
???????????????????????????????????????
```

## User Workflow

### Step 1: Navigate to Cart Management
- QBSales user logs in
- Goes to **Sales Tools ? Cart Management**

### Step 2: Select Client
- Uses the client filter dropdown
- Selects the client for which to manage carts

### Step 3: View Active Carts
- Sees existing carts for users in that client
- **"Create Cart"** button appears when client is selected

### Step 4: Create New Cart
- Clicks **"Create Cart"** button
- Dialog shows all users in the client who don't have carts
- Selects a user from the list
- Clicks **"Create Cart"** in dialog

### Step 5: Cart Created
- Empty cart is created for the selected user
- Success message displays
- Cart list refreshes automatically
- New cart appears in the list with 0 items

### Step 6: Manage New Cart
- Clicks **"Manage"** on the newly created cart
- Opens CartEditorDialog
- Can now add items to the cart

## Security & Validation

? **Authorization**: Requires QBSales or Admin role  
? **Client Filtering**: Only shows users from selected client  
? **Duplicate Prevention**: Returns Conflict if cart already exists  
? **User Validation**: Validates user exists before creation  
? **Audit Logging**: Logs who created which cart and when  

## Error Handling

| Error Scenario | Handling |
|----------------|----------|
| User not found | Returns 404 Not Found |
| Cart already exists | Returns 409 Conflict |
| No users without carts | Shows info message, no dialog |
| API failure | Shows error snackbar with details |
| Network error | Catches exception, shows error |

## Use Cases

### Use Case 1: New Client User
- Client adds new employee
- Employee doesn't have a cart yet
- QBSales rep creates cart and pre-loads items
- Employee starts first punchout with ready cart

### Use Case 2: Seasonal Preparation
- Client has seasonal workers
- Workers return but carts were cleared
- QBSales rep quickly creates new carts
- Pre-loads common items for the season

### Use Case 3: Bulk User Onboarding
- Client adds multiple new users
- QBSales rep selects client
- Creates carts one by one from the list
- Prepares all carts with standard items

## Testing Checklist

- [ ] Create cart button appears when client is selected
- [ ] Create cart button hidden when no client selected
- [ ] Dialog shows only users without carts
- [ ] Dialog shows appropriate message if all users have carts
- [ ] User selection highlights properly
- [ ] Create button disabled until user selected
- [ ] Cart creation succeeds and shows success message
- [ ] Cart list refreshes after creation
- [ ] Newly created cart appears in the list
- [ ] Clicking "Manage" on new cart opens editor
- [ ] Duplicate creation returns conflict error
- [ ] Cancel button closes dialog without action

## API Examples

### Get Users for Client
```http
GET /api/qbsales/carts/client/5/users
Authorization: Bearer {token}

Response 200 OK:
[
  {
    "userId": "abc-123",
    "userEmail": "john.doe@client.com",
    "userName": "John Doe",
    "hasCart": false,
    "cartId": null
  },
  {
    "userId": "def-456",
    "userEmail": "jane.smith@client.com",
    "userName": "Jane Smith",
    "hasCart": true,
    "cartId": 42
  }
]
```

### Create Cart
```http
POST /api/qbsales/carts/user/abc-123/create
Authorization: Bearer {token}

Response 200 OK:
{
  "shoppingCartEVM": {
    "id": 50,
    "applicationUserId": "abc-123"
  },
  "shoppingCartItemEVMs": {}
}
```

## Future Enhancements

1. **Bulk Cart Creation**: Create carts for multiple users at once
2. **Template Application**: Apply item templates during cart creation
3. **Notification**: Email user when cart is created and ready
4. **Cart Cloning**: Copy existing cart to new user
5. **Auto-Creation**: Option to auto-create carts for new users

## Files Changed/Created

### Created
- `ShopQualityboltWebBlazor\Components\CustomComponents\CreateCartDialog.razor`

### Modified
- `ShopQualityboltWeb\Controllers\Api\QBSalesCartController.cs`
  - Added `GetClientUsers` endpoint
  - Added `CreateCartForUser` endpoint
  - Added `UserCartInfo` DTO class
- `ShopQualityboltWebBlazor\Components\Pages\QBSales\CartManagement.razor`
  - Added "Create Cart" button
  - Added `OpenCreateCartDialog` method
  - Added `CreateCart` method
  - Added `UserCartInfo` DTO class

---

**Status**: ? **COMPLETE**  
**Ready for Testing**: ? **YES**  
**Breaking Changes**: ? **NO**
