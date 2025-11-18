# Client Import API Test Script
# This PowerShell script demonstrates how to use the Client Import endpoint

# Configuration
$apiUrl = "https://localhost:7001/api/clientimport"  # Update with your API URL
$token = "YOUR_ADMIN_JWT_TOKEN_HERE"  # Replace with actual admin token

# Sample import data
$importData = @{
    client = @{
        legacyId = "TEST001"
        name = "Test Client Corporation"
    }
    contractItems = @(
        @{
            customerStkNo = "TEST-BOLT-001"
            description = "Test Hex Bolt 1/4-20 x 1.0"
            price = 0.25
            nonStock = $false
            sku = @{
                name = "TEST-HEX-1/4-20-1.0"
                diameter = @{
                    name = "1/4"
                    displayName = "1/4`""
                    value = 0.25
                }
                length = @{
                    name = "1.0"
                    displayName = "1.0`""
                    value = 1.0
                }
                productID = @{
                    legacyId = 99999
                    legacyName = "TEST-PRODUCT"
                    description = "Test Product"
                    group = @{
                        legacyId = "TESTGRP"
                        name = "TEST_BOLTS"
                        displayName = "Test Bolts"
                        description = "Test bolt group"
                        class = @{
                            legacyId = "TESTCLS"
                            name = "TEST_FASTENERS"
                            displayName = "Test Fasteners"
                            description = "Test fastener class"
                        }
                    }
                    shape = @{
                        name = "HEX"
                        displayName = "Hexagon"
                        description = "Hexagonal head"
                    }
                    material = @{
                        name = "STEEL"
                        displayName = "Steel"
                        description = "Carbon steel"
                    }
                    coating = @{
                        name = "ZINC"
                        displayName = "Zinc Plated"
                        description = "Zinc electroplating"
                    }
                    thread = @{
                        name = "UNC"
                        displayName = "UNC"
                        description = "Unified National Coarse"
                    }
                    spec = @{
                        name = "GRADE5"
                        displayName = "Grade 5"
                        description = "SAE Grade 5"
                    }
                }
            }
            diameter = @{
                name = "1/4"
                displayName = "1/4`""
                value = 0.25
            }
            length = @{
                name = "1.0"
                displayName = "1.0`""
                value = 1.0
            }
        }
    )
}

# Convert to JSON
$jsonBody = $importData | ConvertTo-Json -Depth 10

# Set headers
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

Write-Host "Sending import request to: $apiUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "Request Body:" -ForegroundColor Yellow
Write-Host $jsonBody -ForegroundColor Gray
Write-Host ""

try {
    # Send the request
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Headers $headers -Body $jsonBody -SkipCertificateCheck
    
    Write-Host "Import Successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  Client: $($response.clientName) (ID: $($response.clientId))" -ForegroundColor White
    Write-Host "  New Client: $($response.isNewClient)" -ForegroundColor White
    Write-Host "  Imported: $($response.importedItemsCount) items" -ForegroundColor Green
    Write-Host "  Skipped: $($response.skippedItems.Count) items" -ForegroundColor Yellow
    Write-Host "  Failed: $($response.failedItems.Count) items" -ForegroundColor Red
    Write-Host "  Duration: $($response.duration)" -ForegroundColor White
    
    if ($response.skippedItems.Count -gt 0) {
        Write-Host ""
        Write-Host "Skipped Items:" -ForegroundColor Yellow
        $response.skippedItems | ForEach-Object {
            Write-Host "  - $($_.customerStkNo): $($_.reason)" -ForegroundColor Gray
        }
    }
    
    if ($response.failedItems.Count -gt 0) {
        Write-Host ""
        Write-Host "Failed Items:" -ForegroundColor Red
        $response.failedItems | ForEach-Object {
            Write-Host "  - $($_.customerStkNo): $($_.reason)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "Import Failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host ""
        Write-Host "Server Response:" -ForegroundColor Yellow
        Write-Host $_.ErrorDetails.Message -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Script completed." -ForegroundColor Cyan
