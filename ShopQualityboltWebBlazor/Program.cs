using ShopQualityboltWebBlazor.Components;
using MudBlazor.Services;
using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Services.Http;
using QBExternalWebLibrary.Services;
using QBExternalWebLibrary.Services.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using ShopQualityboltWebBlazor.Services;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Ariba;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Components.Server.Circuits;
using QBExternalWebLibrary.Services; // Added this line
using QBExternalWebLibrary.Services.Authentication; // Added this line

var builder = WebApplication.CreateBuilder(args);

// Get API address from configuration based on environment
string apiAddress = builder.Configuration["ApiSettings:BaseAddress"] 
    ?? throw new InvalidOperationException("ApiSettings:BaseAddress is not configured");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Circuit Options for Blazor Server
builder.Services.AddServerSideBlazor(options =>
{
    // Increase the disconnect timeout (default is 3 minutes)
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    
    // Increase the maximum disconnect timeout (how long to keep circuit alive)
    options.DisconnectedCircuitMaxRetained = 100;
    
    // Enable detailed errors in development
    options.DetailedErrors = builder.Environment.IsDevelopment();
    
    // JS interop timeout
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

// Configure SignalR Hub options
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    // Increase message size limits if needed
    options.MaximumReceiveMessageSize = 128 * 1024; // 128 KB (default is 32KB)
    
    // Keep alive interval - how often server pings client
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    
    // Client timeout - how long to wait for client response before considering it disconnected
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    
    // Enable detailed errors in development
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddMudServices();

// Register circuit tracking for token management
builder.Services.AddSingleton<CircuitIdAccessor>();
builder.Services.AddScoped<CircuitHandler, TrackingCircuitHandler>();

// Register Circuit Handler for better connection management
builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();

// Register TokenService for proper per-user token management
builder.Services.AddSingleton<ITokenService, TokenService>();

// Register JwtTokenHandler and CookieHandler as scoped services
builder.Services.AddScoped<JwtTokenHandler>();
builder.Services.AddTransient<CookieHandler>();

// ? Configure SINGLE HttpClient with JWT token support
// Named "Auth" for compatibility with existing services
builder.Services.AddHttpClient("Auth", client =>
{
    client.BaseAddress = new Uri(apiAddress);
})
.AddHttpMessageHandler<CookieHandler>()
.AddHttpMessageHandler<JwtTokenHandler>();

// ? Register default HttpClient that uses "Auth" configuration
// This makes @inject HttpClient use the same client as services
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Auth"));

builder.Services.AddScoped<IApiService<SKU, SKUEditViewModel>, ApiService<SKU, SKUEditViewModel>>();
builder.Services.AddScoped<IApiService<Length, Length>, ApiService<Length, Length>>();
builder.Services.AddScoped<IApiService<Diameter, Diameter>, ApiService<Diameter, Diameter>>();
builder.Services.AddScoped<IApiService<ProductID, ProductID>, ApiService<ProductID, ProductID>>();
builder.Services.AddScoped<IApiService<ContractItem, ContractItemEditViewModel>, ApiService<ContractItem, ContractItemEditViewModel>>();
builder.Services.AddScoped<IApiService<Shape, Shape>, ApiService<Shape, Shape>>();
builder.Services.AddScoped<IApiService<Material, Material>, ApiService<Material, Material>>();
builder.Services.AddScoped<IApiService<Coating, Coating>, ApiService<Coating, Coating>>();
builder.Services.AddScoped<IApiService<Thread, Thread>, ApiService<Thread, Thread>>();
builder.Services.AddScoped<IApiService<Spec, Spec>, ApiService<Spec, Spec>>();
builder.Services.AddScoped<IApiService<ShoppingCart, ShoppingCartEVM>, ApiService<ShoppingCart, ShoppingCartEVM>>();
builder.Services.AddScoped<IApiService<Client, ClientEditViewModel>, ApiService<Client, ClientEditViewModel>>();
builder.Services.AddScoped<ShoppingCartItemApiService>();
builder.Services.AddScoped<PunchOutSessionApiService>();
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ShoppingCartPageApiService>(); // Changed from Singleton
builder.Services.AddScoped<ShoppingCartManagementService>(); // Changed from Singleton
builder.Services.AddScoped<PunchOutManagementService>(); // Changed from Singleton
//builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthenticationApiService, IdentityApiService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.Name = "auth_token";
        options.LoginPath = "/login";
        options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddScoped(sp => (IAccountManagement)sp.GetRequiredService<AuthenticationStateProvider>());
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure Content Security Policy to allow Ariba to frame this site
// MUST be first middleware before HSTS to prevent conflicts
app.Use(async (context, next) =>
{
    // This needs to run before the response is generated
    context.Response.OnStarting(() =>
    {
        // Get Ariba domains from configuration
        var aribaOrigins = builder.Configuration.GetSection("Ariba:AllowedFrameOrigins").Get<string[]>() 
            ?? new[] 
            { 
                "https://*.ariba.com",
                "https://*.sap.com",
                "https://s1.ariba.com",
                "https://service.ariba.com"
            };
        
        // Build frame-ancestors directive
        var frameAncestors = string.Join(" ", new[] { "'self'" }.Concat(aribaOrigins));
        
        // Remove any existing CSP and X-Frame-Options headers
        context.Response.Headers.Remove("Content-Security-Policy");
        context.Response.Headers.Remove("X-Frame-Options");
        
        // Set the CSP header to allow framing from Ariba
        context.Response.Headers["Content-Security-Policy"] = $"frame-ancestors {frameAncestors}";
        
        return Task.CompletedTask;
    });
    
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
