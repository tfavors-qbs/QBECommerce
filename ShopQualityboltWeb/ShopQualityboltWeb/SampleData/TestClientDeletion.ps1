# Client Deletion Test Script
# This PowerShell script demonstrates how to delete a client using the API

# Configuration
$apiUrl = "https://localhost:7237/api/clients"
$token = "YOUR_ADMIN_JWT_TOKEN_HERE"  # Replace with actual admin token
$clientIdToDelete = 0  # Replace with actual client ID

# Prompt for client ID if not set
if ($clientIdToDelete -eq 0) {
    $clientIdToDelete = Read-Host "Enter the Client ID to delete"
}

# Confirm deletion
Write-Host ""
Write-Host "WARNING: This will delete the client and all associated contract items!" -ForegroundColor Red
Write-Host "Users will be disassociated but their accounts will remain active." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Are you sure you want to delete Client ID $clientIdToDelete? (yes/no)"

if ($confirmation -ne "yes") {
    Write-Host "Deletion cancelled." -ForegroundColor Yellow
    exit
}

# Set headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""
Write-Host "Fetching client details first..." -ForegroundColor Cyan

# Step 1: Get client details (optional - for verification)
try {
    $clientDetails = Invoke-RestMethod -Uri "$apiUrl/$clientIdToDelete" -Method Get -Headers $headers -SkipCertificateCheck
    Write-Host "Client found:" -ForegroundColor Green
    Write-Host "  ID: $($clientDetails.id)" -ForegroundColor White
    Write-Host "  Name: $($clientDetails.name)" -ForegroundColor White
    Write-Host "  Legacy ID: $($clientDetails.legacyId)" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "Error fetching client details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Client may not exist or you may not have permission." -ForegroundColor Yellow
    exit
}

# Final confirmation with client name
$finalConfirmation = Read-Host "Type the client name '$($clientDetails.name)' to confirm deletion"
if ($finalConfirmation -ne $clientDetails.name) {
    Write-Host "Confirmation failed. Deletion cancelled." -ForegroundColor Yellow
    exit
}

Write-Host ""
Write-Host "Sending deletion request to: $apiUrl/$clientIdToDelete" -ForegroundColor Cyan

# Step 2: Delete the client
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/$clientIdToDelete" -Method Delete -Headers $headers -SkipCertificateCheck
    
    Write-Host ""
    Write-Host "? Deletion Successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response Details:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  Client: $($response.clientName) (ID: $($response.clientId))" -ForegroundColor White
    Write-Host "  Contract Items Deleted: $($response.deletedContractItemsCount)" -ForegroundColor Green
    Write-Host "  Users Disassociated: $($response.disassociatedUsersCount)" -ForegroundColor Yellow
    Write-Host "  Message: $($response.message)" -ForegroundColor White
    
    if ($response.disassociatedUsersCount -gt 0) {
        Write-Host ""
        Write-Host "Note: $($response.disassociatedUsersCount) user(s) were disassociated from this client." -ForegroundColor Yellow
        Write-Host "Their accounts remain active but they are no longer linked to a client." -ForegroundColor Yellow
    }
}
catch {
    Write-Host ""
    Write-Host "? Deletion Failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host ""
        Write-Host "Server Response:" -ForegroundColor Yellow
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "  Success: $($errorResponse.success)" -ForegroundColor Red
            Write-Host "  Message: $($errorResponse.message)" -ForegroundColor White
            if ($errorResponse.deletedContractItemsCount) {
                Write-Host "  Contract Items Deleted: $($errorResponse.deletedContractItemsCount)" -ForegroundColor White
            }
            if ($errorResponse.disassociatedUsersCount) {
                Write-Host "  Users Disassociated: $($errorResponse.disassociatedUsersCount)" -ForegroundColor White
            }
        }
        catch {
            Write-Host $_.ErrorDetails.Message -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "  - 401 Unauthorized: Invalid or expired token" -ForegroundColor Gray
    Write-Host "  - 403 Forbidden: User does not have Admin role" -ForegroundColor Gray
    Write-Host "  - 404 Not Found: Client ID does not exist" -ForegroundColor Gray
    Write-Host "  - 500 Internal Server Error: Database or server error" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Script completed." -ForegroundColor Cyan
