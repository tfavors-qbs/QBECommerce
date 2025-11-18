# Client Management UI Documentation

## Overview
Admin interface for managing clients with full CRUD operations and safe deletion with impact assessment.

## Access
**URL**: `/admin/clients`  
**Authorization**: Admin role required

## Features

### 1. Client List View
- Displays all clients in a searchable table
- Shows:
  - Client ID
  - Client Name
  - Legacy ID
- Search functionality filters by name or legacy ID
- Sortable columns
- Pagination support

### 2. Create New Client
**Button**: "New Client" (green, top right)

**Dialog Fields**:
- **Client Name** (required)
- **Legacy ID** (required) - Unique identifier from legacy system

**Validation**:
- Both fields are required
- Legacy ID must be unique

### 3. Edit Client
**Button**: Edit icon (blue pencil) in Actions column

**Functionality**:
- Opens dialog with pre-filled client data
- Same fields as create
- Updates client information

### 4. Delete Client (Advanced)
**Button**: Delete icon (red trash) in Actions column

#### Safety Features:
1. **Impact Assessment**
   - Shows count of contract items to be deleted
   - Shows count of users to be disassociated
   - Loads real-time statistics

2. **Confirmation Required**
   - User must type exact client name to proceed
   - Prevents accidental deletions

3. **Clear Communication**
   - Warning: "This action cannot be undone!"
   - Explains what will happen:
     - Contract items permanently deleted
     - Users disassociated but accounts remain active
     - Users can be reassigned later

4. **Detailed Results**
   - Shows success/failure message
   - Reports:
     - Number of contract items deleted
     - Number of users disassociated
     - Total operation time

### 5. View Contract Items
**Button**: Inventory icon (blue) in Actions column

**Functionality**:
- Navigates to `/admin/contract-items?clientId={id}`
- Pre-selects the client
- Auto-loads contract items
- Provides "Back to Clients" button

## User Interface

### Main Page Layout
```
???????????????????????????????????????????????????????
? Client Management                    [New Client]   ?
???????????????????????????????????????????????????????
? [Search...]                                         ?
??????????????????????????????????????????????????????
? ID ? Name             ? Legacy ID   ? Actions      ?
??????????????????????????????????????????????????????
? 1  ? Acme Corp        ? ACME001     ? ? ?? ??      ?
? 2  ? Global Inc       ? GLB002      ? ? ?? ??      ?
??????????????????????????????????????????????????????
```

### Delete Confirmation Dialog
```
???????????????????????????????????????????????????
? ? Warning: This action cannot be undone!       ?
???????????????????????????????????????????????????
? You are about to delete:                        ?
?                                                 ?
? ?? Acme Corporation                             ?
?    Legacy ID: ACME001                           ?
?                                                 ?
? Impact Assessment:                              ?
? • 150 contract items will be permanently deleted?
? • 3 user(s) will be disassociated              ?
?                                                 ?
? To confirm, type the client name:              ?
? [________________]                              ?
?                                                 ?
?            [Cancel]  [Delete Client]            ?
???????????????????????????????????????????????????
```

## Action Buttons

| Icon | Color | Action | Description |
|------|-------|--------|-------------|
| ? (Edit) | Blue | Edit | Modify client details |
| ?? (Inventory) | Blue | View Items | Navigate to contract items |
| ?? (Delete) | Red | Delete | Delete client with safety checks |

## Workflows

### Creating a New Client
1. Click "New Client" button
2. Enter client name
3. Enter unique legacy ID
4. Click "Create"
5. Client appears in list

### Editing a Client
1. Click edit icon for desired client
2. Modify name or legacy ID
3. Click "Update"
4. Changes saved and list refreshed

### Deleting a Client (Safe Process)
1. Click delete icon
2. **Wait for impact assessment** to load
3. **Review the numbers**:
   - How many contract items will be deleted?
   - How many users will be affected?
4. **Type the exact client name** in confirmation field
5. Click "Delete Client" (only enabled after correct name entered)
6. View detailed results in success message

### Viewing Client's Contract Items
1. Click inventory icon
2. Automatically navigates to contract items page
3. Client pre-selected
4. Contract items auto-loaded
5. Use "Back to Clients" to return

## Technical Details

### Components
- **`AdminClients.razor`** - Main page component
- **`ClientDialog.razor`** - Create/Edit dialog
- **`ClientDeleteDialog.razor`** - Delete confirmation dialog with impact assessment

### API Endpoints Used
- `GET /api/clients` - Load all clients
- `POST /api/clients` - Create new client
- `PUT /api/clients/{id}` - Update client
- `DELETE /api/clients/{id}` - Delete client (with cascading)
- `GET /api/contractitems/admin/client/{id}` - Get contract items count
- `GET /api/users` - Get users count

### State Management
- Real-time search filtering
- Lazy loading of impact statistics
- Automatic list refresh after operations

## Error Handling

### Common Scenarios
- **401 Unauthorized**: User not logged in ? Redirect to login
- **403 Forbidden**: User not admin ? Show access denied
- **Duplicate Legacy ID**: Shows validation error
- **Failed Deletion**: Shows error message with details
- **Network Error**: Shows connection error

### User Feedback
- ? Success: Green snackbar with details
- ?? Warning: Yellow snackbar for warnings  
- ? Error: Red snackbar with error message
- ?? Info: Blue snackbar for information

## Best Practices

### Before Deleting a Client
1. ? Verify you have the correct client
2. ? Check the impact numbers carefully
3. ? Consider exporting/backing up data first
4. ? Notify affected users if needed
5. ? Type the name exactly as shown

### After Deleting a Client
1. ? Review the success message
2. ? Check disassociated users
3. ? Reassign users to new clients if needed
4. ? Verify no orphaned data

## Security
- ? Admin-only access enforced
- ? Cookie-based authentication
- ? Confirmation required for deletions
- ? Transaction-safe operations
- ? Detailed audit logging (server-side)

## Navigation
- From **Admin Menu**: Click "Client Management"
- From **Contract Items**: Click "Back to Clients" button
- To **Contract Items**: Click inventory icon for specific client

## Future Enhancements
Potential improvements:
- Bulk operations (delete multiple clients)
- Export client list to CSV/Excel
- Client activity history
- Audit log viewer
- Client usage statistics dashboard
- Import clients from file
