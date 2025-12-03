# ProductID Bulk Import API Documentation

## Endpoint
`POST /api/productids/import`

## Authentication
Requires `Admin` role authorization.

## Description
Creates multiple ProductIDs in a single request. Each ProductID must specify all required dimension properties by their exact names (case-insensitive lookup).

## Request Format

### Content-Type
`application/json`

### Request Body Structure

```json
{
  "productIDs": [
    {
      "legacyId": 12345,
      "legacyName": "BOLT-001",
      "description": "Optional description of the product",
      "groupName": "Structural Bolts",
      "shapeName": "Hex Head",
      "materialName": "Carbon Steel",
      "coatingName": "Zinc Plated",
      "threadName": "Coarse",
      "specName": "ASTM A307"
    },
    {
      "legacyId": 12346,
      "legacyName": "BOLT-002",
      "description": null,
      "groupName": "Structural Bolts",
      "shapeName": "Hex Head",
      "materialName": "Stainless Steel",
      "coatingName": "Plain",
      "threadName": "Fine",
      "specName": "ASTM A193"
    }
  ]
}
```

## Request Model Specifications

### ProductIDImportRequest
| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `productIDs` | `List<ProductIDImportDto>` | Yes | Array of ProductIDs to create |

### ProductIDImportDto
| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `legacyId` | `int` | Yes | Legacy numeric identifier |
| `legacyName` | `string` | Yes | Unique legacy name/code |
| `description` | `string` | No | Optional product description |
| `groupName` | `string` | Yes | Name of the Group (must exist in system) |
| `shapeName` | `string` | Yes | Name of the Shape (must exist in system) |
| `materialName` | `string` | Yes | Name of the Material (must exist in system) |
| `coatingName` | `string` | Yes | Name of the Coating (must exist in system) |
| `threadName` | `string` | Yes | Name of the Thread (must exist in system) |
| `specName` | `string` | Yes | Name of the Spec (must exist in system) |

## Response Format

### Success Response (200 OK)

```json
{
  "success": true,
  "message": "Import completed: 2 created, 0 skipped, 0 failed",
  "totalRequested": 2,
  "successfullyCreated": 2,
  "skipped": 0,
  "failed": 0,
  "results": [
    {
      "legacyName": "BOLT-001",
      "legacyId": 12345,
      "success": true,
      "status": "Created",
      "reason": null,
      "createdProductIDId": 567
    },
    {
      "legacyName": "BOLT-002",
      "legacyId": 12346,
      "success": true,
      "status": "Created",
      "reason": null,
      "createdProductIDId": 568
    }
  ],
  "startTime": "2025-12-02T10:30:00Z",
  "endTime": "2025-12-02T10:30:02Z",
  "duration": "00:00:02"
}
```

### Partial Success Response (200 OK)
When some items succeed and others fail/skip:

```json
{
  "success": false,
  "message": "Import completed: 1 created, 1 skipped, 1 failed",
  "totalRequested": 3,
  "successfullyCreated": 1,
  "skipped": 1,
  "failed": 1,
  "results": [
    {
      "legacyName": "BOLT-001",
      "legacyId": 12345,
      "success": true,
      "status": "Created",
      "reason": null,
      "createdProductIDId": 567
    },
    {
      "legacyName": "BOLT-002",
      "legacyId": 12346,
      "success": false,
      "status": "Skipped",
      "reason": "ProductID with LegacyName 'BOLT-002' already exists",
      "createdProductIDId": null
    },
    {
      "legacyName": "BOLT-003",
      "legacyId": 12347,
      "success": false,
      "status": "Failed",
      "reason": "Material 'Unknown Material' not found",
      "createdProductIDId": null
    }
  ],
  "startTime": "2025-12-02T10:30:00Z",
  "endTime": "2025-12-02T10:30:02Z",
  "duration": "00:00:02"
}
```

### Error Response (400 Bad Request)

```json
{
  "success": false,
  "message": "No ProductIDs provided in request",
  "totalRequested": 0,
  "successfullyCreated": 0,
  "skipped": 0,
  "failed": 0,
  "results": [],
  "startTime": "2025-12-02T10:30:00Z",
  "endTime": "2025-12-02T10:30:00Z",
  "duration": "00:00:00"
}
```

## Response Model Specifications

### ProductIDImportResponse
| Property | Type | Description |
|----------|------|-------------|
| `success` | `bool` | Overall success (true if no failures) |
| `message` | `string` | Summary message |
| `totalRequested` | `int` | Total number of ProductIDs in request |
| `successfullyCreated` | `int` | Number successfully created |
| `skipped` | `int` | Number skipped (already exist) |
| `failed` | `int` | Number failed (validation/lookup errors) |
| `results` | `List<ProductIDImportResult>` | Individual results for each ProductID |
| `startTime` | `DateTime` | UTC timestamp when import started |
| `endTime` | `DateTime` | UTC timestamp when import completed |
| `duration` | `TimeSpan` | Total processing duration |

### ProductIDImportResult
| Property | Type | Description |
|----------|------|-------------|
| `legacyName` | `string` | The LegacyName from the request |
| `legacyId` | `int` | The LegacyId from the request |
| `success` | `bool` | Whether this specific ProductID was created |
| `status` | `string` | "Created", "Skipped", or "Failed" |
| `reason` | `string` | Explanation for skipped/failed items |
| `createdProductIDId` | `int?` | Database ID of created ProductID (null if not created) |

## Important Notes

1. **Case-Insensitive Lookups**: All dimension names (Group, Shape, Material, etc.) are matched case-insensitively
2. **Duplicate Prevention**: ProductIDs with existing LegacyName values will be skipped
3. **Atomic per Item**: Each ProductID is processed independently; failures don't affect other items in the batch
4. **Pre-requisites**: All referenced dimensions (Groups, Shapes, Materials, Coatings, Threads, Specs) must exist in the system before import
5. **Performance**: The endpoint pre-loads all lookup data to optimize batch processing

## Common Error Reasons

- `"ProductID with LegacyName 'XXX' already exists"` - Duplicate detection
- `"Group 'XXX' not found"` - Referenced Group doesn't exist
- `"Shape 'XXX' not found"` - Referenced Shape doesn't exist
- `"Material 'XXX' not found"` - Referenced Material doesn't exist
- `"Coating 'XXX' not found"` - Referenced Coating doesn't exist
- `"Thread 'XXX' not found"` - Referenced Thread doesn't exist
- `"Spec 'XXX' not found"` - Referenced Spec doesn't exist
- `"Exception: [details]"` - Unexpected system error

## Example C# Client Code

```csharp
using System.Net.Http.Json;

public class ProductIDImportClient
{
    private readonly HttpClient _httpClient;
    
    public ProductIDImportClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<ProductIDImportResponse> ImportProductIDsAsync(
        List<ProductIDImportDto> productIDs,
        string authToken)
    {
        var request = new ProductIDImportRequest
        {
            ProductIDs = productIDs
        };
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        
        var response = await _httpClient.PostAsJsonAsync(
            "/api/productids/import", 
            request);
        
        return await response.Content.ReadFromJsonAsync<ProductIDImportResponse>();
    }
}

// Usage
var client = new ProductIDImportClient(httpClient);
var productIDs = new List<ProductIDImportDto>
{
    new ProductIDImportDto
    {
        LegacyId = 12345,
        LegacyName = "BOLT-001",
        Description = "Test bolt",
        GroupName = "Structural Bolts",
        ShapeName = "Hex Head",
        MaterialName = "Carbon Steel",
        CoatingName = "Zinc Plated",
        ThreadName = "Coarse",
        SpecName = "ASTM A307"
    }
};

var result = await client.ImportProductIDsAsync(productIDs, "your-auth-token");
Console.WriteLine($"Created: {result.SuccessfullyCreated}, Failed: {result.Failed}");
```

## Example Python Client Code

```python
import requests
from typing import List, Dict, Any

class ProductIDImportClient:
    def __init__(self, base_url: str, auth_token: str):
        self.base_url = base_url
        self.headers = {
            "Authorization": f"Bearer {auth_token}",
            "Content-Type": "application/json"
        }
    
    def import_product_ids(self, product_ids: List[Dict[str, Any]]) -> Dict[str, Any]:
        url = f"{self.base_url}/api/productids/import"
        payload = {"productIDs": product_ids}
        
        response = requests.post(url, json=payload, headers=self.headers)
        response.raise_for_status()
        
        return response.json()

# Usage
client = ProductIDImportClient("https://your-api.com", "your-auth-token")
product_ids = [
    {
        "legacyId": 12345,
        "legacyName": "BOLT-001",
        "description": "Test bolt",
        "groupName": "Structural Bolts",
        "shapeName": "Hex Head",
        "materialName": "Carbon Steel",
        "coatingName": "Zinc Plated",
        "threadName": "Coarse",
        "specName": "ASTM A307"
    }
]

result = client.import_product_ids(product_ids)
print(f"Created: {result['successfullyCreated']}, Failed: {result['failed']}")
```

## C# Class Definitions for Client Applications

```csharp
using System.ComponentModel.DataAnnotations;

namespace YourClientApp.Models
{
    public class ProductIDImportRequest
    {
        [Required]
        public List<ProductIDImportDto> ProductIDs { get; set; } = new();
    }

    public class ProductIDImportDto
    {
        [Required]
        public int LegacyId { get; set; }
        
        [Required]
        public string LegacyName { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string GroupName { get; set; }
        
        [Required]
        public string ShapeName { get; set; }
        
        [Required]
        public string MaterialName { get; set; }
        
        [Required]
        public string CoatingName { get; set; }
        
        [Required]
        public string ThreadName { get; set; }
        
        [Required]
        public string SpecName { get; set; }
    }

    public class ProductIDImportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalRequested { get; set; }
        public int SuccessfullyCreated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<ProductIDImportResult> Results { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class ProductIDImportResult
    {
        public string LegacyName { get; set; }
        public int LegacyId { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
        public string? Reason { get; set; }
        public int? CreatedProductIDId { get; set; }
    }
}
```
