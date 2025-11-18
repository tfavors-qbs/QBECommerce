# Client Import API Endpoint

## Overview
The Client Import API endpoint allows administrators to import a client and all their associated contract items (customer stock) in a single transaction. The endpoint automatically handles all foreign key dependencies and creates any missing reference data.

## Endpoint Details

**URL:** `POST /api/clientimport`  
**Authorization:** Admin role required  
**Content-Type:** `application/json`

## Database Dependencies

The endpoint automatically resolves and creates the following dependencies in order:

### Hierarchy of Dependencies:
1. **Client** (root entity)
2. **ContractItem** (requires: Client, SKU, Diameter, Length)
   - **SKU** (requires: ProductID, Diameter, Length)
     - **ProductID** (requires: Group, Shape, Material, Coating, Thread, Spec)
       - **Group** (requires: Class)
         - **Class** (root reference data)
       - **Shape** (root reference data)
       - **Material** (root reference data)
       - **Coating** (root reference data)
       - **Thread** (root reference data)
       - **Spec** (root reference data)
   - **Diameter** (root reference data)
   - **Length** (root reference data)

## Request Structure

```json
{
  "client": {
    "legacyId": "string",      // Unique identifier from legacy system
    "name": "string"            // Client name
  },
  "contractItems": [
    {
      "customerStkNo": "string",     // Customer stock number (unique per client)
      "description": "string",        // Item description
      "price": 0.00,                  // Item price
      "nonStock": false,              // Whether item is non-stock
      "sku": {                        // Optional SKU information
        "name": "string",
        "diameter": {
          "name": "string",
          "displayName": "string",
          "value": 0.0             // Numeric value for sorting
        },
        "length": {                // Optional
          "name": "string",
          "displayName": "string",
          "value": 0.0
        },
        "productID": {
          "legacyId": 0,
          "legacyName": "string",
          "description": "string",
          "group": {
            "legacyId": "string",
            "name": "string",
            "displayName": "string",
            "description": "string",
            "class": {
              "legacyId": "string",
              "name": "string",
              "displayName": "string",
              "description": "string"
            }
          },
          "shape": { "name": "string", "displayName": "string", "description": "string" },
          "material": { "name": "string", "displayName": "string", "description": "string" },
          "coating": { "name": "string", "displayName": "string", "description": "string" },
          "thread": { "name": "string", "displayName": "string", "description": "string" },
          "spec": { "name": "string", "displayName": "string", "description": "string" }
        }
      },
      "diameter": {                  // Optional, separate from SKU diameter
        "name": "string",
        "displayName": "string",
        "value": 0.0
      },
      "length": {                    // Optional
        "name": "string",
        "displayName": "string",
        "value": 0.0
      }
    }
  ]
}
```

## Response Structure

```json
{
  "success": true,
  "message": "Successfully imported 10 contract items. Skipped: 0, Failed: 0",
  "clientName": "Acme Corporation",
  "clientId": 123,
  "isNewClient": true,
  "importedItemsCount": 10,
  "skippedItems": [],
  "failedItems": [],
  "startTime": "2024-01-15T10:30:00Z",
  "endTime": "2024-01-15T10:30:05Z",
  "duration": "00:00:05"
}
```

### Response Fields:
- **success**: Whether the overall import succeeded
- **message**: Summary message
- **clientName**: Name of the imported client
- **clientId**: Database ID of the client
- **isNewClient**: True if a new client was created, false if existing
- **importedItemsCount**: Number of contract items successfully imported
- **skippedItems**: Array of items that were skipped (e.g., duplicates)
- **failedItems**: Array of items that failed to import with reasons
- **duration**: Time taken for the import

## Features

### Duplicate Handling
- **Client**: Matches existing clients by `LegacyId`. Reuses existing client if found.
- **Contract Items**: Skips items with duplicate `CustomerStkNo` for the same client
- **Reference Data**: Matches by `Name` or `LegacyId` and reuses existing records

### Transaction Safety
- All operations are wrapped in a database transaction
- If any critical error occurs, all changes are rolled back
- Individual contract item failures don't rollback the entire transaction

### Automatic Creation
The endpoint automatically creates any missing reference data:
- Classes, Groups, Shapes, Materials, Coatings, Threads, Specs
- Diameters and Lengths (with numeric values for sorting)
- ProductIDs and SKUs

## Usage Examples

### Example 1: Basic Import
```bash
curl -X POST https://api.example.com/api/clientimport \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d @ClientImportExample.json
```

### Example 2: Minimal Contract Item (No SKU)
```json
{
  "client": {
    "legacyId": "CLIENT002",
    "name": "Simple Corp"
  },
  "contractItems": [
    {
      "customerStkNo": "SIMPLE-001",
      "description": "Generic Part",
      "price": 10.50,
      "nonStock": true
    }
  ]
}
```

### Example 3: With Existing Reference Data
If Classes, Groups, Materials, etc. already exist in the database, the endpoint will automatically find and reuse them. You don't need to query the database first - just provide the names and the endpoint handles matching.

## Error Handling

### HTTP Status Codes:
- **200 OK**: Import completed (check response for skipped/failed items)
- **400 Bad Request**: Invalid request format or validation errors
- **401 Unauthorized**: Not authenticated
- **403 Forbidden**: Not an admin user
- **500 Internal Server Error**: Server error (transaction rolled back)

### Common Errors:
1. **Duplicate Customer Stock Number**: Item skipped if same `CustomerStkNo` exists for client
2. **Missing Required Fields**: Validation error returned
3. **Invalid Foreign Key**: Error if required dependencies can't be created

## Best Practices

1. **Test with Small Batches**: Start with a small number of items to verify data format
2. **Check Response Details**: Review `skippedItems` and `failedItems` arrays
3. **Use Consistent Naming**: Keep reference data names consistent across imports
4. **Include Descriptions**: Provide descriptions for better data quality
5. **Validate Prices**: Ensure prices are positive decimal values

## Performance Considerations

- Large imports (100+ items) may take several seconds
- Each item triggers multiple database lookups
- Consider batching very large imports (1000+ items) into multiple requests
- Database indexes on `Name` and `LegacyId` columns improve performance

## See Also

- Sample import file: `SampleData/ClientImportExample.json`
- ContractItems API: `/api/contractitems`
- Clients API: `/api/clients`
