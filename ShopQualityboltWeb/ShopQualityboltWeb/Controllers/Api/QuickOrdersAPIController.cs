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
}
