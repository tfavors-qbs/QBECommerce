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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7237") });
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
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddTransient<CookieHandler>();
//builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthenticationApiService, IdentityApiService>();

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.Configuration["FrontendUrl"] ?? "https://localhost:7237") });

// configure client for auth interactions
builder.Services.AddHttpClient(
    "Auth",
    opt => opt.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? "https://localhost:7237"))
    .AddHttpMessageHandler<CookieHandler>();

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
