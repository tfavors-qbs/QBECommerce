using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using QBExternalWebLibrary.Services.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using QBExternalWebLibrary.Models.Catalog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
	options.Cookie.HttpOnly = true; // Security
	options.Cookie.IsEssential = true; // Required for GDPR compliance
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>().AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();

builder.Services.AddScoped<IRepository<Coating>, EFRepository<Coating>>();
builder.Services.AddScoped<IModelService<Coating, Coating?>, ModelService<Coating, Coating?>>();
builder.Services.AddScoped<IModelMapper<Coating, Coating>, GenericMapper<Coating, Coating>>();

builder.Services.AddScoped<IRepository<Class>, EFRepository<Class>>();
builder.Services.AddScoped<IModelService<Class, Class?>, ModelService<Class, Class?>>();
builder.Services.AddScoped<IModelMapper<Class, Class>, GenericMapper<Class, Class>>();

builder.Services.AddScoped<IRepository<Diameter>, EFRepository<Diameter>>();
builder.Services.AddScoped<IModelService<Diameter, Diameter?>, ModelService<Diameter, Diameter?>>();
builder.Services.AddScoped<IModelMapper<Diameter, Diameter>, GenericMapper<Diameter, Diameter>>();


builder.Services.AddScoped<IRepository<Length>, EFRepository<Length>>();
builder.Services.AddScoped<IModelService<Length, Length?>, ModelService<Length, Length?>>();
builder.Services.AddScoped<IModelMapper<Length, Length>, GenericMapper<Length, Length>>();

builder.Services.AddScoped<IRepository<Material>, EFRepository<Material>>();
builder.Services.AddScoped<IModelService<Material, Material?>, ModelService<Material, Material?>>();
builder.Services.AddScoped<IModelMapper<Material, Material>, GenericMapper<Material, Material>>();

builder.Services.AddScoped<IRepository<Shape>, EFRepository<Shape>>();
builder.Services.AddScoped<IModelService<Shape, Shape?>, ModelService<Shape, Shape?>>();
builder.Services.AddScoped<IModelMapper<Shape, Shape>, GenericMapper<Shape, Shape>>();

builder.Services.AddScoped<IRepository<Spec>, EFRepository<Spec>>();
builder.Services.AddScoped<IModelService<Spec, Spec?>, ModelService<Spec, Spec?>>();
builder.Services.AddScoped<IModelMapper<Spec, Spec>, GenericMapper<Spec, Spec>>();

builder.Services.AddScoped<IRepository<Thread>, EFRepository<Thread>>();
builder.Services.AddScoped<IModelService<Thread, Thread?>, ModelService<Thread, Thread?>>();
builder.Services.AddScoped<IModelMapper<Thread, Thread>, GenericMapper<Thread, Thread>>();

builder.Services.AddScoped<IRepository<Client>, ClientEFRepository>();
builder.Services.AddScoped<IModelService<Client, ClientEditViewModel?>, ModelService<Client, ClientEditViewModel?>>();
builder.Services.AddScoped<IModelMapper<Client, ClientEditViewModel>, ClientMapper>();

builder.Services.AddScoped<IRepository<SKU>, SKUEFRepository>();
builder.Services.AddScoped<IModelService<SKU, SKUEditViewModel?>, ModelService<SKU, SKUEditViewModel?>>();
builder.Services.AddScoped<IModelMapper<SKU, SKUEditViewModel>, SKUMapper>();

builder.Services.AddScoped<IRepository<Group>, GroupEFRepository>();
builder.Services.AddScoped<IModelService<Group, GroupEditViewModel?>, ModelService<Group, GroupEditViewModel?>>();
builder.Services.AddScoped<IModelMapper<Group, GroupEditViewModel>, GroupMapper>();

builder.Services.AddScoped<IRepository<ProductID>, ProductIDEFRepository>();
builder.Services.AddScoped<IModelService<ProductID, ProductIDEditViewModel?>, ModelService<ProductID, ProductIDEditViewModel?>>();
builder.Services.AddScoped<IModelMapper<ProductID, ProductIDEditViewModel>, ProductIDMapper>();

builder.Services.AddScoped<IRepository<ContractItem>, ContractItemEFRepository>();
builder.Services.AddScoped<IModelService<ContractItem, ContractItemEditViewModel?>, ModelService<ContractItem, ContractItemEditViewModel?>>();
builder.Services.AddScoped<IModelMapper<ContractItem, ContractItemEditViewModel>, ContractItemMapper>();

builder.Services.AddScoped<IRepository<ShoppingCart>, ShoppingCartEFRepository>();
builder.Services.AddScoped<IModelService<ShoppingCart, ShoppingCartEVM?>, ModelService<ShoppingCart, ShoppingCartEVM?>>();
builder.Services.AddScoped<IModelMapper<ShoppingCart, ShoppingCartEVM>, ShoppingCartMapper>();

builder.Services.AddScoped<IRepository<ShoppingCartItem>, ShoppingCartItemEFRepository>();
builder.Services.AddScoped<IModelService<ShoppingCartItem, ShoppingCartItemEVM?>, ModelService<ShoppingCartItem, ShoppingCartItemEVM?>>();
builder.Services.AddScoped<IModelMapper<ShoppingCartItem, ShoppingCartItemEVM>, ShoppingCartItemMapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapIdentityApi<ApplicationUser>();

app.UseHttpsRedirection();
app.UseSession();
app.UseStaticFiles();

app.MapGet("/roles", (ClaimsPrincipal user) => {
    if (user.Identity is not null && user.Identity.IsAuthenticated) {
        var identity = (ClaimsIdentity)user.Identity;
        var roles = identity.FindAll(identity.RoleClaimType)
            .Select(c =>
                new {
                    c.Issuer,
                    c.OriginalIssuer,
                    c.Type,
                    c.Value,
                    c.ValueType
                });

        return TypedResults.Json(roles);
    }

    return Results.Unauthorized();
}).RequireAuthorization();


app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager, [FromBody] object empty) => {
    if (empty != null) {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }
    return Results.Unauthorized();
}).WithOpenApi().RequireAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
