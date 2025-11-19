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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using QBExternalWebLibrary.Services.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Ariba;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        // Get allowed origins from configuration
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://localhost:44318", "http://localhost:5000" }; // Default for development
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // Important for authentication
              .WithExposedHeaders("Authorization"); // Allow Authorization header
    });
    
    // Separate policy for development (more permissive)
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container - API only
builder.Services.AddControllers();

// Add distributed memory cache for session support
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
	options.Cookie.HttpOnly = true; // Security
	options.Cookie.IsEssential = true; // Required for GDPR compliance
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };
    
    // For cookie-based authentication fallback
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Allow token from Authorization header (default)
            // Also check for token in cookie for backward compatibility
            if (context.Request.Cookies.ContainsKey("access_token"))
            {
                context.Token = context.Request.Cookies["access_token"];
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie(options =>
{
    options.Cookie.Name = "auth_token";
    options.LoginPath = "/login";
    options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
    options.AccessDeniedPath = "/access-denied";
    options.Cookie.SameSite = SameSiteMode.Lax; // Cookie auth for non-PunchOut users
});

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

builder.Services.AddScoped<IRepository<PunchOutSession>, EFRepository<PunchOutSession>>();
builder.Services.AddScoped<IModelService<PunchOutSession, PunchOutSession?>, ModelService<PunchOutSession, PunchOutSession?>>();
builder.Services.AddScoped<IModelMapper<PunchOutSession, PunchOutSession>, GenericMapper<PunchOutSession, PunchOutSession>>();

var app = builder.Build();

// Automatically apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database migration...");
        var context = services.GetRequiredService<DataContext>();
        
        // Apply any pending migrations
        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                pendingMigrations.Count(), 
                string.Join(", ", pendingMigrations));
            
            context.Database.Migrate();
            logger.LogInformation("Database migration completed successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations found");
        }
        
        // Seed roles
        logger.LogInformation("Seeding roles...");
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "Admin", "User", "QBSales" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                logger.LogInformation("Creating role: {Role}", role);
                await roleManager.CreateAsync(new IdentityRole(role));
            }
            else
            {
                logger.LogInformation("Role already exists: {Role}", role);
            }
        }
        
        logger.LogInformation("Role seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        logger.LogError("Connection String (sanitized): Server={Server};Database={Database}", 
            builder.Configuration.GetConnectionString("DefaultConnectionString")?.Split(';')[0],
            builder.Configuration.GetConnectionString("DefaultConnectionString")?.Split(';')[1]);
        
        // Don't throw - let the app start so we can see the error in logs
        // But log it prominently
        logger.LogCritical("DATABASE MIGRATION FAILED - Application may not function correctly!");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/api/error"); // API error endpoint instead of MVC
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapIdentityApi<ApplicationUser>();

app.UseHttpsRedirection();
app.UseSession();
app.UseStaticFiles();

// Enable CORS (MUST be before UseRouting/UseAuthentication/UseAuthorization)
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("AllowBlazorApp");
}

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

// Map API controllers only
app.MapControllers();

app.Run();
