# HttpClient Configuration - Clean Architecture

## ?? **Single Source of Truth**

The application now uses **ONE consistent HttpClient configuration** with proper JWT token handling.

---

## ??? **Configuration (Program.cs)**

```csharp
// Register token infrastructure
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<JwtTokenHandler>();
builder.Services.AddTransient<CookieHandler>();

// ? DEFAULT HttpClient - Used by @inject HttpClient
builder.Services.AddHttpClient<HttpClient>(client =>
{
    client.BaseAddress = new Uri(apiAddress);
})
.AddHttpMessageHandler<CookieHandler>()
.AddHttpMessageHandler<JwtTokenHandler>();

// ? NAMED "Auth" client - Used by IHttpClientFactory.CreateClient("Auth")
builder.Services.AddHttpClient("Auth")
    .ConfigureHttpClient(client => 
    {
        client.BaseAddress = new Uri(apiAddress);
    })
    .AddHttpMessageHandler<CookieHandler>()
    .AddHttpMessageHandler<JwtTokenHandler>();
```

---

## ?? **How It Works**

### **1. Default HttpClient**
- Used when you inject `HttpClient` directly in components/services
- Example: `@inject HttpClient Http`
- **Handlers Applied:**
  1. `CookieHandler` - manages cookies (if needed)
  2. `JwtTokenHandler` - adds JWT token to Authorization header

### **2. Named "Auth" HttpClient**
- Used by `CookieAuthenticationStateProvider` via `IHttpClientFactory.CreateClient("Auth")`
- Has **identical configuration** to default client
- Exists only for explicit auth operations

### **3. Both Share Same Token Handling**
- Both clients use the **same `JwtTokenHandler` instance per circuit**
- Both read from the **same `TokenService` instance per circuit**
- **Consistent behavior** across all API calls

---

## ?? **Request Flow**

```
Component/Service
    ?
HttpClient (injected)
    ?
CookieHandler (if cookies needed)
    ?
JwtTokenHandler
    ?
TokenService.GetTokenAsync()
    ?
Add "Authorization: Bearer {token}" header
    ?
Send request to API
    ?
Check response for X-Token-Refreshed header
    ?
If refreshed: TokenService.SetTokenAsync(newToken)
    ?
Trigger OnTokenChanged event
    ?
Update UI (AuthenticationStateChanged)
```

---

## ?? **Usage Examples**

### **In Blazor Components**
```csharp
@inject HttpClient Http

private async Task LoadData()
{
    // Automatically includes JWT token
    var response = await Http.GetAsync("api/products");
    
    if (response.IsSuccessStatusCode)
    {
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
    }
}
```

### **In Services (ApiService)**
```csharp
public class ApiService<T, TEdit>
{
    private readonly HttpClient _httpClient;
    
    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient; // Already configured with token handler
    }
    
    public async Task<T?> GetByIdAsync(int id)
    {
        // Token automatically included
        return await _httpClient.GetFromJsonAsync<T>($"api/{_entityName}/{id}");
    }
}
```

### **In Auth Provider**
```csharp
public class CookieAuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    
    public CookieAuthenticationStateProvider(IHttpClientFactory httpClientFactory)
    {
        // Uses named "Auth" client, but same configuration
        _httpClient = httpClientFactory.CreateClient("Auth");
    }
}
```

---

## ? **Benefits of This Approach**

1. **Consistency** 
   - All HTTP calls use same token handling
   - No confusion about which client to use

2. **Simplicity**
   - Single configuration to maintain
   - Easy to understand and debug

3. **DRY (Don't Repeat Yourself)**
   - Token handling logic in one place (`JwtTokenHandler`)
   - Configuration defined once

4. **Automatic Token Refresh**
   - All requests check for refreshed tokens
   - UI automatically updates when token changes

5. **Security**
   - Tokens stored per-circuit (per-user)
   - No shared state between users
   - Automatic cleanup when circuit closes

---

## ?? **What NOT to Do**

### ? **Don't Create Raw HttpClient Instances**
```csharp
// BAD - bypasses token handler
var client = new HttpClient();
client.BaseAddress = new Uri("https://api.example.com");
```

### ? **Don't Register Multiple Conflicting Clients**
```csharp
// BAD - confusing and error-prone
builder.Services.AddScoped(sp => new HttpClient { ... });
builder.Services.AddHttpClient("Client1", ...);
builder.Services.AddHttpClient("Client2", ...);
```

### ? **Don't Manually Add Authorization Headers**
```csharp
// BAD - JwtTokenHandler already does this
var token = await TokenService.GetTokenAsync();
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

---

## ?? **Debugging**

### **Check Token in Debug Page**
Navigate to `/debug/token` to verify:
- ? Token exists in TokenService
- ? Token is being sent with requests
- ? API calls succeed (200 OK)

### **Check Logs**
Look for these log messages:
- `[TokenService] ? Token saved to memory (length: 1196)`
- `[JwtTokenHandler] ? Authorization header added`
- `[JwtTokenHandler] ?? Server sent refreshed token`

### **Common Issues**

| Symptom | Cause | Solution |
|---------|-------|----------|
| 401 Unauthorized | Token not being sent | Check `/debug/token` page |
| Token null | Not logged in | Login first |
| Old claims | Token not refreshed | Check for `X-Token-Refreshed` header |
| Different users see same data | Global token variable | Should not happen anymore |

---

## ?? **Related Documentation**

- `TokenHandlingArchitecture_FIXED.md` - Token storage architecture
- `StaleClaimsFixSummary.md` - Token refresh mechanism
- `TokenDebug.razor` - Debug page for troubleshooting

---

## ?? **Summary**

? **One HttpClient configuration** with JWT token support  
? **Consistent behavior** across all API calls  
? **Automatic token inclusion** in all requests  
? **Automatic token refresh** from server  
? **Per-user isolation** via scoped services  
? **Simple and maintainable** architecture  

This is the **correct, production-ready** approach for Blazor Server + JWT authentication! ??
