# Client Deletion API Documentation

## Endpoint
`DELETE /api/clients/{id}`

## Authorization
**Admin role required**

## Description
Deletes a client and handles all related data according to business rules:
1. **Deletes all contract items** (customer stock) associated with the client
2. **Disassociates all users** from the client (sets their `ClientId` to `null`) but keeps the user accounts active
3. **Deletes the client** record

## Request

### URL Parameters
- `id` (integer, required): The ID of the client to delete

### Headers
```
Authorization: Bearer {admin-jwt-token}
```
or cookies if using cookie-based authentication

### Example Request
```bash
DELETE https://localhost:7237/api/clients/5
```

## Response

### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Client 'Acme Corporation' deleted successfully",
  "clientId": 5,
  "clientName": "Acme Corporation",
  "deletedContractItemsCount": 150,
  "disassociatedUsersCount": 3
}
```

### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the deletion was successful |
| `message` | string | Human-readable message describing the result |
| `clientId` | integer | The ID of the deleted client |
| `clientName` | string | The name of the deleted client |
| `deletedContractItemsCount` | integer | Number of contract items that were deleted |
| `disassociatedUsersCount` | integer | Number of users that were disassociated from the client |

### Error Responses

#### 404 Not Found
When the client doesn't exist:
```json
{
  "message": "Client not found"
}
```

#### 401 Unauthorized
When not authenticated:
```json
{
  "message": "Unauthorized"
}
```

#### 403 Forbidden
When authenticated but not an admin:
```json
{
  "message": "Forbidden"
}
```

#### 500 Internal Server Error
When deletion fails:
```json
{
  "success": false,
  "message": "Failed to delete client: {error details}",
  "clientId": 5,
  "clientName": "Acme Corporation",
  "deletedContractItemsCount": 0,
  "disassociatedUsersCount": 0
}
```

## Business Rules

### Contract Items (Customer Stock)
? **All contract items are permanently deleted** when a client is deleted.

This includes:
- All product associations
- All pricing information
- All custom stock numbers

?? **Warning**: This action cannot be undone. Make sure to back up data if needed.

### Users
? **Users are preserved** but disassociated from the client.

After deletion:
- User accounts remain active
- Users can still log in
- Users' `ClientId` is set to `null`
- Users' roles and permissions are unchanged
- Users can be associated with a different client later

### Related Data Preserved
The following data is **NOT deleted**:
- SKUs
- ProductIDs
- Classes, Groups
- Shapes, Materials, Coatings, Threads, Specs
- Lengths, Diameters

These are shared reference data that may be used by other clients.

## Transaction Safety

The deletion operation is wrapped in a database transaction:
- ? All-or-nothing operation
- ? Automatic rollback if any step fails
- ? Maintains data integrity

## Logging

The operation logs the following events:
1. Start of deletion process
2. Number of contract items deleted
3. Number of users disassociated
4. Successful completion or error details

Check application logs for detailed information about each deletion.

## Usage Examples

### Using cURL
```bash
curl -X DELETE https://localhost:7237/api/clients/5 \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json"
```

### Using PowerShell
```powershell
$headers = @{
    "Authorization" = "Bearer YOUR_ADMIN_TOKEN"
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod `
    -Uri "https://localhost:7237/api/clients/5" `
    -Method Delete `
    -Headers $headers

Write-Host "Success: $($response.success)"
Write-Host "Message: $($response.message)"
Write-Host "Contract Items Deleted: $($response.deletedContractItemsCount)"
Write-Host "Users Disassociated: $($response.disassociatedUsersCount)"
```

### Using JavaScript/Fetch
```javascript
const response = await fetch('https://localhost:7237/api/clients/5', {
  method: 'DELETE',
  headers: {
    'Authorization': 'Bearer YOUR_ADMIN_TOKEN',
    'Content-Type': 'application/json'
  },
  credentials: 'include' // if using cookies
});

const result = await response.json();
console.log('Deleted:', result.clientName);
console.log('Contract items removed:', result.deletedContractItemsCount);
console.log('Users disassociated:', result.disassociatedUsersCount);
```

## Best Practices

### Before Deleting a Client

1. **Verify the client ID** is correct
2. **Notify affected users** that their client association will be removed
3. **Export/backup data** if needed for records
4. **Check contract item count** to understand data impact

### After Deleting a Client

1. **Verify users** can still log in
2. **Reassign users** to new clients if needed
3. **Archive audit logs** for compliance
4. **Clean up orphaned data** (if any)

### Recommended Workflow

```bash
# 1. Get client details first
GET /api/clients/5

# 2. Verify contract items count
GET /api/contractitems/admin/client/5

# 3. Check associated users
GET /api/users?clientId=5  # if such endpoint exists

# 4. Perform deletion
DELETE /api/clients/5

# 5. Verify users are still accessible
GET /api/users
```

## Security Considerations

- ? **Admin-only**: Only users with Admin role can delete clients
- ? **Audit trail**: All deletions are logged
- ? **Transaction safety**: Ensures data consistency
- ?? **Irreversible**: Deleted contract items cannot be recovered

## Testing

### Test Scenario 1: Delete Client with Data
```bash
# Expected: Success with contract items deleted and users disassociated
DELETE /api/clients/5
```

### Test Scenario 2: Delete Non-Existent Client
```bash
# Expected: 404 Not Found
DELETE /api/clients/99999
```

### Test Scenario 3: Delete as Non-Admin
```bash
# Expected: 403 Forbidden
DELETE /api/clients/5  # without admin token
```

## Troubleshooting

### Issue: "Client not found"
- Verify the client ID exists
- Check if it was already deleted

### Issue: Transaction timeout
- May occur with clients that have thousands of contract items
- Consider implementing batch deletion for large datasets

### Issue: Users still showing client association
- Ensure users refresh their authentication
- Check application cache
- Verify database was updated

## Related Endpoints

- `GET /api/clients` - List all clients
- `GET /api/clients/{id}` - Get client details
- `POST /api/clients` - Create a new client
- `PUT /api/clients/{id}` - Update client information
- `GET /api/contractitems/admin/client/{id}` - Get client's contract items
- `GET /api/users` - List all users

## Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial implementation with cascading deletion logic |
