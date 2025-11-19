# JWT Configuration Required

## ACTION REQUIRED: Add to appsettings.json

Add the following section to your `appsettings.json` and `appsettings.Production.json`:

```json
{
  "Jwt": {
    "Key": "YourSecretKeyHereMustBeAtLeast32CharactersLongForHS256Algorithm",
    "Issuer": "https://api.qualitybolt.com",
    "Audience": "https://shop.qualitybolt.com",
    "ExpireMinutes": "30"
  }
}
```

### Important Notes:

1. **Jwt:Key**
   - MUST be at least 32 characters long
   - Use a strong random string
   - Keep it SECRET - never commit to source control
   - Different for each environment (Dev, Staging, Production)
   
   Generate a strong key:
   ```powershell
   # PowerShell
   [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
   ```

2. **Jwt:Issuer**
   - Your API domain
   - Examples:
     - Dev: `https://localhost:7001`
     - Prod: `https://api.qualitybolt.com`

3. **Jwt:Audience**
   - Your Blazor app domain
   - Examples:
     - Dev: `https://localhost:7169`
     - Prod: `https://shop.qualitybolt.com`

4. **Jwt:ExpireMinutes**
   - Should match PunchOut session timeout
   - Default: `30` minutes
   - Don't make it too long for security

## Example Configurations

### Development (appsettings.Development.json):
```json
{
  "Jwt": {
    "Key": "DevelopmentSecretKey32CharsOrMoreForTestingOnly!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7169",
    "ExpireMinutes": "30"
  }
}
```

### Production (appsettings.Production.json):
```json
{
  "Jwt": {
    "Key": "ProductionSecretKeyShouldBeInKeyVaultNotHere!",
    "Issuer": "https://api.qualitybolt.com",
    "Audience": "https://shop.qualitybolt.com",
    "ExpireMinutes": "30"
  }
}
```

## Azure Configuration (Recommended)

Instead of storing the key in appsettings, use Azure Key Vault or App Settings:

### Azure App Service ? Configuration:
```
Name: Jwt__Key
Value: <your-secret-key>
```

### Access in code:
```csharp
// No changes needed - Configuration automatically reads from Azure App Settings
var key = _config["Jwt:Key"]; // Works with both appsettings and Azure
```

## Verify Configuration

After adding the JWT section, restart your API and test:

```bash
# Check if configuration is loaded
dotnet run

# Should see no errors about missing Jwt:Key, Jwt:Issuer, etc.
```

## Security Checklist

- [ ] JWT Key is at least 32 characters
- [ ] Different key for each environment
- [ ] Key NOT committed to source control
- [ ] Production key stored in Azure Key Vault
- [ ] Issuer matches API domain
- [ ] Audience matches Blazor domain
- [ ] Token expiration is reasonable (30 minutes)

---

**Priority**: ?? **CRITICAL**  
**Required Before**: Testing Ariba PunchOut flow  
**Applies To**: `ShopQualityboltWeb` API project
