# Quick Order Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement Quick Orders feature allowing users to save cart configurations for reuse.

**Architecture:** New QuickOrder, QuickOrderItem, and QuickOrderTag entities with full CRUD API. Blazor pages for user management and QBSales administration. Follows existing ShoppingCart patterns.

**Tech Stack:** ASP.NET Core 9.0, Entity Framework Core, Blazor Server, MudBlazor

---

## Phase 1: Data Models

### Task 1.1: Create QuickOrder Model

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Models/Catalog/QuickOrder.cs`

**Step 1: Create the model file**

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models.Catalog;

public class QuickOrder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ForeignKey("Owner")]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;

    public bool IsSharedClientWide { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public int TimesUsed { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<QuickOrderItem>? Items { get; set; }
    public List<QuickOrderTag>? Tags { get; set; }
}

public class QuickOrderEVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public bool IsSharedClientWide { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int TimesUsed { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsOwner { get; set; }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Models/Catalog/QuickOrder.cs
git commit -m "feat: add QuickOrder model and EVM"
```

---

### Task 1.2: Create QuickOrderItem Model

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Models/Catalog/QuickOrderItem.cs`

**Step 1: Create the model file**

```csharp
namespace QBExternalWebLibrary.Models.Catalog;

public class QuickOrderItem
{
    public int Id { get; set; }
    public int QuickOrderId { get; set; }
    public QuickOrder QuickOrder { get; set; } = null!;
    public int ContractItemId { get; set; }
    public ContractItem ContractItem { get; set; } = null!;
    public int Quantity { get; set; }
}

public class QuickOrderItemEVM
{
    public int Id { get; set; }
    public int QuickOrderId { get; set; }
    public int ContractItemId { get; set; }
    public ContractItemEditViewModel? ContractItem { get; set; }
    public int Quantity { get; set; }
    public bool IsAvailable { get; set; } = true;

    public QuickOrderItemEVM Copy()
    {
        return new QuickOrderItemEVM
        {
            Id = Id,
            QuickOrderId = QuickOrderId,
            ContractItemId = ContractItemId,
            ContractItem = ContractItem,
            Quantity = Quantity,
            IsAvailable = IsAvailable
        };
    }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Models/Catalog/QuickOrderItem.cs
git commit -m "feat: add QuickOrderItem model and EVM"
```

---

### Task 1.3: Create QuickOrderTag Model

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Models/Catalog/QuickOrderTag.cs`

**Step 1: Create the model file**

```csharp
namespace QBExternalWebLibrary.Models.Catalog;

public class QuickOrderTag
{
    public int Id { get; set; }
    public int QuickOrderId { get; set; }
    public QuickOrder QuickOrder { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Models/Catalog/QuickOrderTag.cs
git commit -m "feat: add QuickOrderTag model"
```

---

### Task 1.4: Create QuickOrderPageEVM

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Models/Pages/QuickOrderPageEVM.cs`

**Step 1: Create the page EVM file**

```csharp
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Pages;

public class QuickOrderPageEVM
{
    public List<QuickOrderEVM> MyQuickOrders { get; set; } = new();
    public List<QuickOrderEVM> SharedQuickOrders { get; set; } = new();
    public List<string> AllTags { get; set; } = new();
}

public class QuickOrderDetailEVM
{
    public QuickOrderEVM QuickOrder { get; set; } = null!;
    public List<QuickOrderItemEVM> Items { get; set; } = new();
    public List<string> AvailableTags { get; set; } = new();
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Models/Pages/QuickOrderPageEVM.cs
git commit -m "feat: add QuickOrderPageEVM and QuickOrderDetailEVM"
```

---

## Phase 2: Database Setup

### Task 2.1: Add DbSets to DataContext

**Files:**
- Modify: `QBExternalWebLibrary/QBExternalWebLibrary/Data/DataContext.cs`

**Step 1: Add DbSet properties**

Add after existing DbSets:

```csharp
public DbSet<QuickOrder> QuickOrders { get; set; }
public DbSet<QuickOrderItem> QuickOrderItems { get; set; }
public DbSet<QuickOrderTag> QuickOrderTags { get; set; }
```

**Step 2: Add using statement if needed**

Add at top: `using QBExternalWebLibrary.Models.Catalog;`

**Step 3: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Data/DataContext.cs
git commit -m "feat: add QuickOrder DbSets to DataContext"
```

---

### Task 2.2: Create Database Migration

**Files:**
- Create: Migration file (auto-generated)

**Step 1: Generate migration**

Run from solution directory:
```bash
dotnet ef migrations add AddQuickOrders --project QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj --startup-project ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj
```

Expected: Migration file created in Migrations folder

**Step 2: Review generated migration**

Verify it creates:
- QuickOrders table with all columns
- QuickOrderItems table with FKs to QuickOrders and ContractItems
- QuickOrderTags table with FK to QuickOrders

**Step 3: Apply migration**

Run:
```bash
dotnet ef database update --project QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj --startup-project ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj
```

Expected: Database updated successfully

**Step 4: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Migrations/
git commit -m "feat: add QuickOrders database migration"
```

---

## Phase 3: Mappers

### Task 3.1: Create QuickOrderMapper

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Models/Mapping/QuickOrderMapper.cs`

**Step 1: Create mapper file**

```csharp
using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Mapping;

public class QuickOrderMapper : IModelMapper<QuickOrder, QuickOrderEVM>
{
    private readonly IRepository<QuickOrder> _repository;

    public QuickOrderMapper(IRepository<QuickOrder> repository)
    {
        _repository = repository;
    }

    public QuickOrder MapToModel(QuickOrderEVM view)
    {
        var quickOrder = _repository.GetById(view.Id);
        if (quickOrder == null)
        {
            quickOrder = new QuickOrder
            {
                Id = view.Id,
                Name = view.Name,
                OwnerId = view.OwnerId,
                IsSharedClientWide = view.IsSharedClientWide,
                CreatedAt = view.CreatedAt,
                LastUsedAt = view.LastUsedAt,
                TimesUsed = view.TimesUsed,
                IsDeleted = view.IsDeleted,
                DeletedAt = view.DeletedAt
            };
        }
        else
        {
            quickOrder.Name = view.Name;
            quickOrder.IsSharedClientWide = view.IsSharedClientWide;
            quickOrder.LastUsedAt = view.LastUsedAt;
            quickOrder.TimesUsed = view.TimesUsed;
            quickOrder.IsDeleted = view.IsDeleted;
            quickOrder.DeletedAt = view.DeletedAt;
        }
        return quickOrder;
    }

    public QuickOrderEVM MapToEdit(QuickOrder model)
    {
        return new QuickOrderEVM
        {
            Id = model.Id,
            Name = model.Name,
            OwnerId = model.OwnerId,
            OwnerName = model.Owner != null ? $"{model.Owner.GivenName} {model.Owner.FamilyName}".Trim() : null,
            OwnerEmail = model.Owner?.Email,
            IsSharedClientWide = model.IsSharedClientWide,
            CreatedAt = model.CreatedAt,
            LastUsedAt = model.LastUsedAt,
            TimesUsed = model.TimesUsed,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt,
            ItemCount = model.Items?.Count ?? 0,
            TotalValue = model.Items?.Sum(i => i.Quantity * (i.ContractItem?.Price ?? 0)) ?? 0,
            Tags = model.Tags?.Select(t => t.Tag).ToList() ?? new()
        };
    }

    public List<QuickOrderEVM> MapToEdit(IEnumerable<QuickOrder> models)
    {
        return models.Select(MapToEdit).ToList();
    }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Models/Mapping/QuickOrderMapper.cs
git commit -m "feat: add QuickOrderMapper"
```

---

### Task 3.2: Create QuickOrderItemMapper

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Models/Mapping/QuickOrderItemMapper.cs`

**Step 1: Create mapper file**

```csharp
using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Mapping;

public class QuickOrderItemMapper : IModelMapper<QuickOrderItem, QuickOrderItemEVM>
{
    private readonly IRepository<QuickOrderItem> _repository;
    private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;

    public QuickOrderItemMapper(
        IRepository<QuickOrderItem> repository,
        IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper)
    {
        _repository = repository;
        _contractItemMapper = contractItemMapper;
    }

    public QuickOrderItem MapToModel(QuickOrderItemEVM view)
    {
        var item = _repository.GetById(view.Id);
        if (item == null)
        {
            item = new QuickOrderItem
            {
                Id = view.Id,
                QuickOrderId = view.QuickOrderId,
                ContractItemId = view.ContractItemId,
                Quantity = view.Quantity
            };
        }
        else
        {
            item.QuickOrderId = view.QuickOrderId;
            item.ContractItemId = view.ContractItemId;
            item.Quantity = view.Quantity;
        }
        return item;
    }

    public QuickOrderItemEVM MapToEdit(QuickOrderItem model)
    {
        return new QuickOrderItemEVM
        {
            Id = model.Id,
            QuickOrderId = model.QuickOrderId,
            ContractItemId = model.ContractItemId,
            ContractItem = model.ContractItem != null ? _contractItemMapper.MapToEdit(model.ContractItem) : null,
            Quantity = model.Quantity,
            IsAvailable = model.ContractItem != null
        };
    }

    public List<QuickOrderItemEVM> MapToEdit(IEnumerable<QuickOrderItem> models)
    {
        return models.Select(MapToEdit).ToList();
    }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Models/Mapping/QuickOrderItemMapper.cs
git commit -m "feat: add QuickOrderItemMapper"
```

---

## Phase 4: Service Registration

### Task 4.1: Register QuickOrder Services in API Program.cs

**Files:**
- Modify: `ShopQualityboltWeb/ShopQualityboltWeb/Program.cs`

**Step 1: Add service registrations**

Find the section with other model service registrations and add:

```csharp
// QuickOrder services
builder.Services.AddScoped<IRepository<QuickOrder>, Repository<QuickOrder>>();
builder.Services.AddScoped<IRepository<QuickOrderItem>, Repository<QuickOrderItem>>();
builder.Services.AddScoped<IRepository<QuickOrderTag>, Repository<QuickOrderTag>>();
builder.Services.AddScoped<IModelMapper<QuickOrder, QuickOrderEVM>, QuickOrderMapper>();
builder.Services.AddScoped<IModelMapper<QuickOrderItem, QuickOrderItemEVM>, QuickOrderItemMapper>();
builder.Services.AddScoped<IModelService<QuickOrder, QuickOrderEVM>, ModelService<QuickOrder, QuickOrderEVM>>();
builder.Services.AddScoped<IModelService<QuickOrderItem, QuickOrderItemEVM>, ModelService<QuickOrderItem, QuickOrderItemEVM>>();
builder.Services.AddScoped<IModelService<QuickOrderTag, QuickOrderTag>, ModelService<QuickOrderTag, QuickOrderTag>>();
```

**Step 2: Add using statements**

```csharp
using QBExternalWebLibrary.Models.Catalog;
```

**Step 3: Verify file compiles**

Run: `dotnet build ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add ShopQualityboltWeb/ShopQualityboltWeb/Program.cs
git commit -m "feat: register QuickOrder services in DI container"
```

---

## Phase 5: API Controllers

### Task 5.1: Create QuickOrdersAPIController - Basic CRUD

**Files:**
- Create: `ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs`

**Step 1: Create controller with list and get endpoints**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models.Pages;
using QBExternalWebLibrary.Services.Model;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api;

[Route("api/quickorders")]
[ApiController]
[Authorize]
public class QuickOrdersAPIController : ControllerBase
{
    private readonly IModelService<QuickOrder, QuickOrderEVM> _quickOrderService;
    private readonly IModelService<QuickOrderItem, QuickOrderItemEVM> _quickOrderItemService;
    private readonly IModelService<QuickOrderTag, QuickOrderTag> _tagService;
    private readonly IModelService<ContractItem, ContractItemEditViewModel?> _contractItemService;
    private readonly IModelMapper<QuickOrder, QuickOrderEVM> _mapper;
    private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<QuickOrdersAPIController> _logger;

    public QuickOrdersAPIController(
        IModelService<QuickOrder, QuickOrderEVM> quickOrderService,
        IModelService<QuickOrderItem, QuickOrderItemEVM> quickOrderItemService,
        IModelService<QuickOrderTag, QuickOrderTag> tagService,
        IModelService<ContractItem, ContractItemEditViewModel?> contractItemService,
        IModelMapper<QuickOrder, QuickOrderEVM> mapper,
        IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper,
        UserManager<ApplicationUser> userManager,
        ILogger<QuickOrdersAPIController> logger)
    {
        _quickOrderService = quickOrderService;
        _quickOrderItemService = quickOrderItemService;
        _tagService = tagService;
        _contractItemService = contractItemService;
        _mapper = mapper;
        _contractItemMapper = contractItemMapper;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<QuickOrderPageEVM>> GetQuickOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var pageEVM = new QuickOrderPageEVM();

        // Get user's own quick orders (not deleted)
        var myOrders = _quickOrderService
            .FindFullyIncluded(q => q.OwnerId == userId && !q.IsDeleted)
            .ToList();
        pageEVM.MyQuickOrders = myOrders.Select(q => MapToEVMWithOwnership(q, userId)).ToList();

        // Get shared quick orders from same client (not deleted, not owned by user)
        if (user.ClientId.HasValue)
        {
            var sharedOrders = _quickOrderService
                .FindFullyIncluded(q => q.IsSharedClientWide
                    && !q.IsDeleted
                    && q.OwnerId != userId
                    && q.Owner.ClientId == user.ClientId)
                .ToList();
            pageEVM.SharedQuickOrders = sharedOrders.Select(q => MapToEVMWithOwnership(q, userId)).ToList();
        }

        // Get all tags user has used
        var allTags = _tagService
            .Find(t => myOrders.Select(o => o.Id).Contains(t.QuickOrderId))
            .Select(t => t.Tag)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
        pageEVM.AllTags = allTags;

        return Ok(pageEVM);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuickOrderDetailEVM>> GetQuickOrder(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var quickOrder = _quickOrderService.FindFullyIncluded(q => q.Id == id).FirstOrDefault();
        if (quickOrder == null) return NotFound("Quick Order not found");

        // Check access: must be owner OR shared + same client
        bool isOwner = quickOrder.OwnerId == userId;
        bool isSharedAndSameClient = quickOrder.IsSharedClientWide
            && user.ClientId.HasValue
            && quickOrder.Owner?.ClientId == user.ClientId;

        if (!isOwner && !isSharedAndSameClient)
            return Forbid();

        // Don't show deleted unless owner
        if (quickOrder.IsDeleted && !isOwner)
            return NotFound("Quick Order not found");

        var detailEVM = new QuickOrderDetailEVM
        {
            QuickOrder = MapToEVMWithOwnership(quickOrder, userId)
        };

        // Get items with availability check
        var items = _quickOrderItemService
            .FindInclude(i => i.QuickOrderId == id, i => i.ContractItem)
            .ToList();

        var clientContractItems = user.ClientId.HasValue
            ? _contractItemService.Find(c => c.ClientId == user.ClientId).ToList()
            : new List<ContractItem>();

        detailEVM.Items = items.Select(item =>
        {
            var contractItem = clientContractItems.FirstOrDefault(c => c.Id == item.ContractItemId);
            return new QuickOrderItemEVM
            {
                Id = item.Id,
                QuickOrderId = item.QuickOrderId,
                ContractItemId = item.ContractItemId,
                ContractItem = contractItem != null ? _contractItemMapper.MapToEdit(contractItem) : null,
                Quantity = item.Quantity,
                IsAvailable = contractItem != null
            };
        }).ToList();

        // Get available tags for autocomplete
        detailEVM.AvailableTags = _tagService
            .Find(t => t.QuickOrder.OwnerId == userId)
            .Select(t => t.Tag)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return Ok(detailEVM);
    }

    private QuickOrderEVM MapToEVMWithOwnership(QuickOrder model, string currentUserId)
    {
        var evm = _mapper.MapToEdit(model);
        evm.IsOwner = model.OwnerId == currentUserId;
        return evm;
    }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs
git commit -m "feat: add QuickOrdersAPIController with list and get endpoints"
```

---

### Task 5.2: Add Create and Update Endpoints

**Files:**
- Modify: `ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs`

**Step 1: Add request DTOs and create endpoint**

Add inside the controller class:

```csharp
public class CreateQuickOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
    public List<QuickOrderItemRequest>? Items { get; set; }
}

public class QuickOrderItemRequest
{
    public int ContractItemId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateQuickOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
}

[HttpPost]
public async Task<ActionResult<QuickOrderEVM>> CreateQuickOrder([FromBody] CreateQuickOrderRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var user = await _userManager.FindByIdAsync(userId);
    if (user == null) return NotFound("User not found");

    if (string.IsNullOrWhiteSpace(request.Name))
        return BadRequest("Name is required");

    // Create quick order
    var quickOrder = new QuickOrder
    {
        Name = request.Name.Trim(),
        OwnerId = userId,
        IsSharedClientWide = request.IsSharedClientWide,
        CreatedAt = DateTime.UtcNow
    };
    _quickOrderService.Create(quickOrder);

    // Add tags
    foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
    {
        _tagService.Create(new QuickOrderTag
        {
            QuickOrderId = quickOrder.Id,
            Tag = tag.Trim()
        });
    }

    // Add items if provided
    if (request.Items != null)
    {
        foreach (var itemReq in request.Items)
        {
            // Validate contract item belongs to user's client
            var contractItem = _contractItemService
                .Find(c => c.Id == itemReq.ContractItemId && c.ClientId == user.ClientId)
                .FirstOrDefault();

            if (contractItem != null)
            {
                _quickOrderItemService.Create(new QuickOrderItem
                {
                    QuickOrderId = quickOrder.Id,
                    ContractItemId = itemReq.ContractItemId,
                    Quantity = itemReq.Quantity
                });
            }
        }
    }

    _logger.LogInformation("User {UserId} created Quick Order {QuickOrderId}: {Name}",
        userId, quickOrder.Id, quickOrder.Name);

    return CreatedAtAction(nameof(GetQuickOrder), new { id = quickOrder.Id },
        MapToEVMWithOwnership(quickOrder, userId));
}

[HttpPut("{id}")]
public async Task<ActionResult<QuickOrderEVM>> UpdateQuickOrder(int id, [FromBody] UpdateQuickOrderRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var quickOrder = _quickOrderService.GetById(id);
    if (quickOrder == null) return NotFound("Quick Order not found");

    // Only owner can update
    if (quickOrder.OwnerId != userId)
        return Forbid();

    if (string.IsNullOrWhiteSpace(request.Name))
        return BadRequest("Name is required");

    // Update basic properties
    quickOrder.Name = request.Name.Trim();
    quickOrder.IsSharedClientWide = request.IsSharedClientWide;
    _quickOrderService.Update(quickOrder);

    // Update tags: remove old, add new
    var existingTags = _tagService.Find(t => t.QuickOrderId == id).ToList();
    _tagService.DeleteRange(existingTags);

    foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
    {
        _tagService.Create(new QuickOrderTag
        {
            QuickOrderId = id,
            Tag = tag.Trim()
        });
    }

    _logger.LogInformation("User {UserId} updated Quick Order {QuickOrderId}", userId, id);

    var updated = _quickOrderService.FindFullyIncluded(q => q.Id == id).First();
    return Ok(MapToEVMWithOwnership(updated, userId));
}
```

**Step 2: Verify file compiles**

Run: `dotnet build ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs
git commit -m "feat: add create and update endpoints to QuickOrdersAPIController"
```

---

### Task 5.3: Add Delete, Copy, and Add-to-Cart Endpoints

**Files:**
- Modify: `ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs`

**Step 1: Add remaining endpoints**

Add inside the controller class:

```csharp
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteQuickOrder(int id)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var quickOrder = _quickOrderService.GetById(id);
    if (quickOrder == null) return NotFound("Quick Order not found");

    // Only owner can delete
    if (quickOrder.OwnerId != userId)
        return Forbid();

    // Soft delete
    quickOrder.IsDeleted = true;
    quickOrder.DeletedAt = DateTime.UtcNow;
    _quickOrderService.Update(quickOrder);

    _logger.LogInformation("User {UserId} deleted Quick Order {QuickOrderId}", userId, id);

    return NoContent();
}

[HttpPost("{id}/copy")]
public async Task<ActionResult<QuickOrderEVM>> CopyQuickOrder(int id)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var user = await _userManager.FindByIdAsync(userId);
    if (user == null) return NotFound("User not found");

    var source = _quickOrderService.FindFullyIncluded(q => q.Id == id).FirstOrDefault();
    if (source == null) return NotFound("Quick Order not found");

    // Check access
    bool isOwner = source.OwnerId == userId;
    bool isSharedAndSameClient = source.IsSharedClientWide
        && user.ClientId.HasValue
        && source.Owner?.ClientId == user.ClientId;

    if (!isOwner && !isSharedAndSameClient)
        return Forbid();

    // Create copy
    var copy = new QuickOrder
    {
        Name = $"{source.Name} (Copy)",
        OwnerId = userId,
        IsSharedClientWide = false, // Copies are private by default
        CreatedAt = DateTime.UtcNow
    };
    _quickOrderService.Create(copy);

    // Copy tags
    if (source.Tags != null)
    {
        foreach (var tag in source.Tags)
        {
            _tagService.Create(new QuickOrderTag
            {
                QuickOrderId = copy.Id,
                Tag = tag.Tag
            });
        }
    }

    // Copy items
    if (source.Items != null)
    {
        foreach (var item in source.Items)
        {
            _quickOrderItemService.Create(new QuickOrderItem
            {
                QuickOrderId = copy.Id,
                ContractItemId = item.ContractItemId,
                Quantity = item.Quantity
            });
        }
    }

    _logger.LogInformation("User {UserId} copied Quick Order {SourceId} to {CopyId}",
        userId, id, copy.Id);

    var result = _quickOrderService.FindFullyIncluded(q => q.Id == copy.Id).First();
    return CreatedAtAction(nameof(GetQuickOrder), new { id = copy.Id },
        MapToEVMWithOwnership(result, userId));
}

public class AddToCartRequest
{
    public List<int>? SelectedItemIds { get; set; } // null means all items
}

[HttpPost("{id}/add-to-cart")]
public async Task<ActionResult<AddToCartResult>> AddToCart(int id, [FromBody] AddToCartRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var user = await _userManager.FindByIdAsync(userId);
    if (user == null) return NotFound("User not found");

    var quickOrder = _quickOrderService.FindFullyIncluded(q => q.Id == id).FirstOrDefault();
    if (quickOrder == null) return NotFound("Quick Order not found");

    // Check access
    bool isOwner = quickOrder.OwnerId == userId;
    bool isSharedAndSameClient = quickOrder.IsSharedClientWide
        && user.ClientId.HasValue
        && quickOrder.Owner?.ClientId == user.ClientId;

    if (!isOwner && !isSharedAndSameClient)
        return Forbid();

    // Get or create shopping cart
    var shoppingCartService = HttpContext.RequestServices
        .GetRequiredService<IModelService<ShoppingCart, ShoppingCartEVM>>();
    var shoppingCartItemService = HttpContext.RequestServices
        .GetRequiredService<IModelService<ShoppingCartItem, ShoppingCartItemEVM?>>();

    var cart = shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
    if (cart == null)
    {
        cart = new ShoppingCart { ApplicationUserId = userId };
        shoppingCartService.Create(cart);
    }

    var existingCartItems = shoppingCartItemService
        .Find(i => i.ShoppingCartId == cart.Id)
        .ToList();

    // Get valid contract items for user's client
    var clientContractItems = user.ClientId.HasValue
        ? _contractItemService.Find(c => c.ClientId == user.ClientId).Select(c => c.Id).ToHashSet()
        : new HashSet<int>();

    int addedCount = 0;
    int skippedCount = 0;

    var itemsToAdd = quickOrder.Items ?? new List<QuickOrderItem>();
    if (request.SelectedItemIds != null)
    {
        itemsToAdd = itemsToAdd.Where(i => request.SelectedItemIds.Contains(i.Id)).ToList();
    }

    foreach (var item in itemsToAdd)
    {
        // Skip unavailable items
        if (!clientContractItems.Contains(item.ContractItemId))
        {
            skippedCount++;
            continue;
        }

        var existingCartItem = existingCartItems
            .FirstOrDefault(ci => ci.ContractItemId == item.ContractItemId);

        if (existingCartItem != null)
        {
            // Add to existing quantity
            existingCartItem.Quantity += item.Quantity;
            shoppingCartItemService.Update(existingCartItem);
        }
        else
        {
            // Create new cart item
            shoppingCartItemService.Create(new ShoppingCartItem
            {
                ShoppingCartId = cart.Id,
                ContractItemId = item.ContractItemId,
                Quantity = item.Quantity
            });
        }
        addedCount++;
    }

    // Update analytics
    quickOrder.LastUsedAt = DateTime.UtcNow;
    quickOrder.TimesUsed++;
    _quickOrderService.Update(quickOrder);

    _logger.LogInformation("User {UserId} added {Count} items from Quick Order {QuickOrderId} to cart",
        userId, addedCount, id);

    return Ok(new AddToCartResult
    {
        AddedCount = addedCount,
        SkippedCount = skippedCount,
        Message = skippedCount > 0
            ? $"Added {addedCount} items ({skippedCount} unavailable items skipped)"
            : $"Added {addedCount} items to cart"
    });
}

public class AddToCartResult
{
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

[HttpGet("tags")]
public async Task<ActionResult<List<string>>> GetTags()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var tags = _tagService
        .Find(t => t.QuickOrder.OwnerId == userId)
        .Select(t => t.Tag)
        .Distinct()
        .OrderBy(t => t)
        .ToList();

    return Ok(tags);
}
```

**Step 2: Add using statement for ShoppingCart**

Ensure at top: `using QBExternalWebLibrary.Models.Catalog;`

**Step 3: Verify file compiles**

Run: `dotnet build ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs
git commit -m "feat: add delete, copy, add-to-cart, and tags endpoints"
```

---

### Task 5.4: Add Item Management Endpoints

**Files:**
- Modify: `ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs`

**Step 1: Add item CRUD endpoints**

Add inside the controller class:

```csharp
[HttpPost("{id}/items")]
public async Task<ActionResult<QuickOrderItemEVM>> AddItem(int id, [FromBody] QuickOrderItemRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var user = await _userManager.FindByIdAsync(userId);
    if (user == null) return NotFound("User not found");

    var quickOrder = _quickOrderService.GetById(id);
    if (quickOrder == null) return NotFound("Quick Order not found");

    if (quickOrder.OwnerId != userId)
        return Forbid();

    // Validate contract item
    var contractItem = _contractItemService
        .Find(c => c.Id == request.ContractItemId && c.ClientId == user.ClientId)
        .FirstOrDefault();

    if (contractItem == null)
        return BadRequest("Invalid or unauthorized contract item");

    // Check if item already exists
    var existingItem = _quickOrderItemService
        .Find(i => i.QuickOrderId == id && i.ContractItemId == request.ContractItemId)
        .FirstOrDefault();

    if (existingItem != null)
    {
        existingItem.Quantity += request.Quantity;
        _quickOrderItemService.Update(existingItem);
        return Ok(new QuickOrderItemEVM
        {
            Id = existingItem.Id,
            QuickOrderId = existingItem.QuickOrderId,
            ContractItemId = existingItem.ContractItemId,
            ContractItem = _contractItemMapper.MapToEdit(contractItem),
            Quantity = existingItem.Quantity,
            IsAvailable = true
        });
    }

    var item = new QuickOrderItem
    {
        QuickOrderId = id,
        ContractItemId = request.ContractItemId,
        Quantity = request.Quantity
    };
    _quickOrderItemService.Create(item);

    return CreatedAtAction(nameof(GetQuickOrder), new { id }, new QuickOrderItemEVM
    {
        Id = item.Id,
        QuickOrderId = item.QuickOrderId,
        ContractItemId = item.ContractItemId,
        ContractItem = _contractItemMapper.MapToEdit(contractItem),
        Quantity = item.Quantity,
        IsAvailable = true
    });
}

[HttpPut("{id}/items/{itemId}")]
public async Task<ActionResult<QuickOrderItemEVM>> UpdateItem(int id, int itemId, [FromBody] QuickOrderItemRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var quickOrder = _quickOrderService.GetById(id);
    if (quickOrder == null) return NotFound("Quick Order not found");

    if (quickOrder.OwnerId != userId)
        return Forbid();

    var item = _quickOrderItemService.GetById(itemId);
    if (item == null || item.QuickOrderId != id)
        return NotFound("Item not found");

    item.Quantity = request.Quantity;
    _quickOrderItemService.Update(item);

    var contractItem = _contractItemService.GetById(item.ContractItemId);

    return Ok(new QuickOrderItemEVM
    {
        Id = item.Id,
        QuickOrderId = item.QuickOrderId,
        ContractItemId = item.ContractItemId,
        ContractItem = contractItem != null ? _contractItemMapper.MapToEdit(contractItem) : null,
        Quantity = item.Quantity,
        IsAvailable = contractItem != null
    });
}

[HttpDelete("{id}/items/{itemId}")]
public async Task<ActionResult> RemoveItem(int id, int itemId)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var quickOrder = _quickOrderService.GetById(id);
    if (quickOrder == null) return NotFound("Quick Order not found");

    if (quickOrder.OwnerId != userId)
        return Forbid();

    var item = _quickOrderItemService.GetById(itemId);
    if (item == null || item.QuickOrderId != id)
        return NotFound("Item not found");

    _quickOrderItemService.Delete(item);

    return NoContent();
}
```

**Step 2: Verify file compiles**

Run: `dotnet build ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QuickOrdersAPIController.cs
git commit -m "feat: add item management endpoints to QuickOrdersAPIController"
```

---

## Phase 6: QBSales API Controller

### Task 6.1: Create QBSalesQuickOrderController

**Files:**
- Create: `ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QBSalesQuickOrderController.cs`

**Step 1: Create the controller**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Services.Model;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api;

[Route("api/qbsales/quickorders")]
[ApiController]
[Authorize(Roles = "QBSales,Admin")]
public class QBSalesQuickOrderController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IModelService<QuickOrder, QuickOrderEVM> _quickOrderService;
    private readonly IModelService<QuickOrderItem, QuickOrderItemEVM> _quickOrderItemService;
    private readonly IModelService<QuickOrderTag, QuickOrderTag> _tagService;
    private readonly IModelMapper<QuickOrder, QuickOrderEVM> _mapper;
    private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<QBSalesQuickOrderController> _logger;

    public QBSalesQuickOrderController(
        DataContext context,
        IModelService<QuickOrder, QuickOrderEVM> quickOrderService,
        IModelService<QuickOrderItem, QuickOrderItemEVM> quickOrderItemService,
        IModelService<QuickOrderTag, QuickOrderTag> tagService,
        IModelMapper<QuickOrder, QuickOrderEVM> mapper,
        IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper,
        UserManager<ApplicationUser> userManager,
        ILogger<QBSalesQuickOrderController> logger)
    {
        _context = context;
        _quickOrderService = quickOrderService;
        _quickOrderItemService = quickOrderItemService;
        _tagService = tagService;
        _mapper = mapper;
        _contractItemMapper = contractItemMapper;
        _userManager = userManager;
        _logger = logger;
    }

    public class UserQuickOrderInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public int QuickOrderCount { get; set; }
        public int DeletedQuickOrderCount { get; set; }
    }

    [HttpGet("users/client/{clientId}")]
    public async Task<ActionResult<List<UserQuickOrderInfo>>> GetUsersByClient(int clientId)
    {
        var users = await _context.Users
            .Include(u => u.Client)
            .Where(u => u.ClientId == clientId)
            .ToListAsync();

        var result = new List<UserQuickOrderInfo>();

        foreach (var user in users)
        {
            var activeCount = _quickOrderService
                .Find(q => q.OwnerId == user.Id && !q.IsDeleted)
                .Count();
            var deletedCount = _quickOrderService
                .Find(q => q.OwnerId == user.Id && q.IsDeleted)
                .Count();

            result.Add(new UserQuickOrderInfo
            {
                UserId = user.Id,
                UserName = $"{user.GivenName ?? ""} {user.FamilyName ?? ""}".Trim(),
                UserEmail = user.Email ?? "",
                ClientId = user.ClientId,
                ClientName = user.Client?.Name,
                QuickOrderCount = activeCount,
                DeletedQuickOrderCount = deletedCount
            });
        }

        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<QuickOrderEVM>>> GetUserQuickOrders(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var quickOrders = _quickOrderService
            .FindFullyIncluded(q => q.OwnerId == userId && !q.IsDeleted)
            .ToList();

        return Ok(quickOrders.Select(q => _mapper.MapToEdit(q)).ToList());
    }

    [HttpGet("user/{userId}/deleted")]
    public async Task<ActionResult<List<QuickOrderEVM>>> GetUserDeletedQuickOrders(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var quickOrders = _quickOrderService
            .FindFullyIncluded(q => q.OwnerId == userId && q.IsDeleted)
            .ToList();

        return Ok(quickOrders.Select(q => _mapper.MapToEdit(q)).ToList());
    }

    public class CreateQuickOrderForUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public bool IsSharedClientWide { get; set; }
        public List<QuickOrderItemRequest>? Items { get; set; }
    }

    public class QuickOrderItemRequest
    {
        public int ContractItemId { get; set; }
        public int Quantity { get; set; }
    }

    [HttpPost("user/{userId}")]
    public async Task<ActionResult<QuickOrderEVM>> CreateQuickOrderForUser(
        string userId,
        [FromBody] CreateQuickOrderForUserRequest request)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null) return NotFound("User not found");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        // Create quick order with target user as owner
        var quickOrder = new QuickOrder
        {
            Name = request.Name.Trim(),
            OwnerId = userId, // Target user is owner
            IsSharedClientWide = request.IsSharedClientWide,
            CreatedAt = DateTime.UtcNow
        };
        _quickOrderService.Create(quickOrder);

        // Add tags
        foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
        {
            _tagService.Create(new QuickOrderTag
            {
                QuickOrderId = quickOrder.Id,
                Tag = tag.Trim()
            });
        }

        // Add items
        if (request.Items != null && targetUser.ClientId.HasValue)
        {
            var validContractItems = await _context.ContractItems
                .Where(c => c.ClientId == targetUser.ClientId)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var itemReq in request.Items)
            {
                if (validContractItems.Contains(itemReq.ContractItemId))
                {
                    _quickOrderItemService.Create(new QuickOrderItem
                    {
                        QuickOrderId = quickOrder.Id,
                        ContractItemId = itemReq.ContractItemId,
                        Quantity = itemReq.Quantity
                    });
                }
            }
        }

        _logger.LogInformation("QBSales user {QBSalesId} created Quick Order {QuickOrderId} for user {UserId}",
            currentUserId, quickOrder.Id, userId);

        var result = _quickOrderService.FindFullyIncluded(q => q.Id == quickOrder.Id).First();
        return CreatedAtAction(nameof(GetUserQuickOrders), new { userId }, _mapper.MapToEdit(result));
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<QuickOrderEVM>> RestoreQuickOrder(int id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var quickOrder = _quickOrderService.GetById(id);
        if (quickOrder == null) return NotFound("Quick Order not found");

        if (!quickOrder.IsDeleted)
            return BadRequest("Quick Order is not deleted");

        quickOrder.IsDeleted = false;
        quickOrder.DeletedAt = null;
        _quickOrderService.Update(quickOrder);

        _logger.LogInformation("QBSales user {QBSalesId} restored Quick Order {QuickOrderId}",
            currentUserId, id);

        var result = _quickOrderService.FindFullyIncluded(q => q.Id == id).First();
        return Ok(_mapper.MapToEdit(result));
    }
}
```

**Step 2: Verify file compiles**

Run: `dotnet build ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ShopQualityboltWeb/ShopQualityboltWeb/Controllers/Api/QBSalesQuickOrderController.cs
git commit -m "feat: add QBSalesQuickOrderController for sales user management"
```

---

## Phase 7: HTTP API Services (Client-Side)

### Task 7.1: Create QuickOrderApiService

**Files:**
- Create: `QBExternalWebLibrary/QBExternalWebLibrary/Services/Http/QuickOrderApiService.cs`

**Step 1: Create the API service**

```csharp
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using System.Net.Http.Json;

namespace QBExternalWebLibrary.Services.Http;

public class QuickOrderApiService
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = "api/quickorders";

    public QuickOrderApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Auth");
    }

    public async Task<QuickOrderPageEVM?> GetAllAsync()
    {
        var response = await _httpClient.GetAsync(Endpoint).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderPageEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderDetailEVM?> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/{id}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderDetailEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderEVM?> CreateAsync(CreateQuickOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(Endpoint, request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderEVM?> UpdateAsync(int id, UpdateQuickOrderRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{Endpoint}/{id}", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"{Endpoint}/{id}").ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<QuickOrderEVM?> CopyAsync(int id)
    {
        var response = await _httpClient.PostAsync($"{Endpoint}/{id}/copy", null).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<AddToCartResult?> AddToCartAsync(int id, List<int>? selectedItemIds = null)
    {
        var request = new AddToCartRequest { SelectedItemIds = selectedItemIds };
        var response = await _httpClient.PostAsJsonAsync($"{Endpoint}/{id}/add-to-cart", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AddToCartResult>().ConfigureAwait(false);
    }

    public async Task<List<string>> GetTagsAsync()
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/tags").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return new List<string>();
        return await response.Content.ReadFromJsonAsync<List<string>>().ConfigureAwait(false) ?? new List<string>();
    }

    public async Task<QuickOrderItemEVM?> AddItemAsync(int quickOrderId, int contractItemId, int quantity)
    {
        var request = new QuickOrderItemRequest { ContractItemId = contractItemId, Quantity = quantity };
        var response = await _httpClient.PostAsJsonAsync($"{Endpoint}/{quickOrderId}/items", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderItemEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderItemEVM?> UpdateItemAsync(int quickOrderId, int itemId, int quantity)
    {
        var request = new QuickOrderItemRequest { Quantity = quantity };
        var response = await _httpClient.PutAsJsonAsync($"{Endpoint}/{quickOrderId}/items/{itemId}", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderItemEVM>().ConfigureAwait(false);
    }

    public async Task<bool> RemoveItemAsync(int quickOrderId, int itemId)
    {
        var response = await _httpClient.DeleteAsync($"{Endpoint}/{quickOrderId}/items/{itemId}").ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}

public class CreateQuickOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
    public List<QuickOrderItemRequest>? Items { get; set; }
}

public class UpdateQuickOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
}

public class QuickOrderItemRequest
{
    public int ContractItemId { get; set; }
    public int Quantity { get; set; }
}

public class AddToCartRequest
{
    public List<int>? SelectedItemIds { get; set; }
}

public class AddToCartResult
{
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

**Step 2: Verify file compiles**

Run: `dotnet build QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add QBExternalWebLibrary/QBExternalWebLibrary/Services/Http/QuickOrderApiService.cs
git commit -m "feat: add QuickOrderApiService for client-side HTTP calls"
```

---

### Task 7.2: Register QuickOrderApiService in Blazor

**Files:**
- Modify: `ShopQualityboltWebBlazor/Program.cs`

**Step 1: Add service registration**

Find the section with other service registrations and add:

```csharp
builder.Services.AddScoped<QuickOrderApiService>();
```

**Step 2: Add using statement**

```csharp
using QBExternalWebLibrary.Services.Http;
```

**Step 3: Verify file compiles**

Run: `dotnet build ShopQualityboltWebBlazor/ShopQualityboltWebBlazor.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add ShopQualityboltWebBlazor/Program.cs
git commit -m "feat: register QuickOrderApiService in Blazor DI"
```

---

## Phase 8: Blazor Pages (Continued in next document)

The remaining phases will cover:
- Phase 8: User-facing Quick Orders page
- Phase 9: Quick Order editor dialog
- Phase 10: Save as Quick Order dialog on Cart page
- Phase 11: QBSales Quick Order management page
- Phase 12: Analytics dashboard
- Phase 13: Navigation updates

---

## Execution Notes

**Working Directory:** `.worktrees/quick-order`

**Build Command:** `dotnet build ShopQualityboltWeb/ShopQualityboltWeb.sln`

**Test Command:** No test project - verify by running application

**Run Application:**
```bash
cd ShopQualityboltWeb/ShopQualityboltWeb
dotnet run
```
