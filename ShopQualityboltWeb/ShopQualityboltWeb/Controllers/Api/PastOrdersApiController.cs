using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models.Pages;
using QBExternalWebLibrary.Services.Model;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api;

[Route("api/past-orders")]
[ApiController]
[Authorize]
public class PastOrdersApiController : ControllerBase
{
    private readonly IModelService<PastOrder, PastOrderEVM> _pastOrderService;
    private readonly IModelService<PastOrderItem, PastOrderItemEVM> _pastOrderItemService;
    private readonly IModelService<PastOrderTag, PastOrderTag> _tagService;
    private readonly IModelService<ContractItem, ContractItemEditViewModel?> _contractItemService;
    private readonly IModelService<ShoppingCartItem, ShoppingCartItemEVM?> _cartItemService;
    private readonly IModelService<ShoppingCart, ShoppingCartEVM?> _cartService;
    private readonly IModelMapper<PastOrder, PastOrderEVM> _mapper;
    private readonly IModelMapper<PastOrderItem, PastOrderItemEVM> _itemMapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IErrorLogService _errorLogService;

    public PastOrdersApiController(
        IModelService<PastOrder, PastOrderEVM> pastOrderService,
        IModelService<PastOrderItem, PastOrderItemEVM> pastOrderItemService,
        IModelService<PastOrderTag, PastOrderTag> tagService,
        IModelService<ContractItem, ContractItemEditViewModel?> contractItemService,
        IModelService<ShoppingCartItem, ShoppingCartItemEVM?> cartItemService,
        IModelService<ShoppingCart, ShoppingCartEVM?> cartService,
        IModelMapper<PastOrder, PastOrderEVM> mapper,
        IModelMapper<PastOrderItem, PastOrderItemEVM> itemMapper,
        UserManager<ApplicationUser> userManager,
        IErrorLogService errorLogService)
    {
        _pastOrderService = pastOrderService;
        _pastOrderItemService = pastOrderItemService;
        _tagService = tagService;
        _contractItemService = contractItemService;
        _cartItemService = cartItemService;
        _cartService = cartService;
        _mapper = mapper;
        _itemMapper = itemMapper;
        _userManager = userManager;
        _errorLogService = errorLogService;
    }

    [HttpGet]
    public async Task<ActionResult<PastOrderPageEVM>> GetPastOrders()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var pageEVM = new PastOrderPageEVM();

            // My Orders: UserId = current user AND ClientId = current user's client
            var myOrders = _pastOrderService
                .FindFullyIncluded(p => p.UserId == userId
                    && !p.IsDeleted
                    && p.ClientId == user.ClientId)
                .ToList();
            pageEVM.MyOrders = myOrders.Select(p => _mapper.MapToEdit(p)).ToList();

            // Organization Orders: ClientId = current user's client AND UserId != current user
            if (user.ClientId.HasValue)
            {
                var orgOrders = _pastOrderService
                    .FindFullyIncluded(p => !p.IsDeleted
                        && p.UserId != userId
                        && p.ClientId == user.ClientId)
                    .ToList();
                pageEVM.OrganizationOrders = orgOrders.Select(p => _mapper.MapToEdit(p)).ToList();
            }

            // Get all tags from user's orders
            var allOrderIds = myOrders.Select(o => o.Id).ToList();
            var allTags = _tagService
                .Find(t => allOrderIds.Contains(t.PastOrderId))
                .Select(t => t.Tag)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
            pageEVM.AllTags = allTags;

            return Ok(pageEVM);
        }
        catch (Exception ex)
        {
            await _errorLogService.LogErrorAsync(
                "Past Order Error",
                "Failed to Get Past Orders",
                ex.Message,
                ex,
                userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                requestUrl: HttpContext.Request.Path,
                httpMethod: HttpContext.Request.Method);
            return StatusCode(500, new { message = "Failed to retrieve past orders" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PastOrderDetailEVM>> GetPastOrder(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var pastOrder = _pastOrderService.FindFullyIncluded(p => p.Id == id).FirstOrDefault();
            if (pastOrder == null) return NotFound("Past Order not found");

            // Check access: ClientId must match user's current ClientId
            if (pastOrder.ClientId != user.ClientId)
                return Forbid();

            if (pastOrder.IsDeleted)
                return NotFound("Past Order not found");

            var detailEVM = new PastOrderDetailEVM
            {
                Order = _mapper.MapToEdit(pastOrder)
            };

            // Get items with availability check
            var items = _pastOrderItemService
                .FindInclude(i => i.PastOrderId == id, i => i.ContractItem)
                .ToList();

            var clientContractItemIds = user.ClientId.HasValue
                ? _contractItemService.Find(c => c.ClientId == user.ClientId).Select(c => c.Id).ToHashSet()
                : new HashSet<int>();

            detailEVM.Items = items.Select(i => {
                var evm = _itemMapper.MapToEdit(i);
                evm.IsAvailable = clientContractItemIds.Contains(i.ContractItemId);
                return evm;
            }).ToList();

            return Ok(detailEVM);
        }
        catch (Exception ex)
        {
            await _errorLogService.LogErrorAsync(
                "Past Order Error",
                "Failed to Get Past Order",
                ex.Message,
                ex,
                additionalData: new { pastOrderId = id },
                userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                requestUrl: HttpContext.Request.Path,
                httpMethod: HttpContext.Request.Method);
            return StatusCode(500, new { message = "Failed to retrieve past order" });
        }
    }

    [HttpPut("{id}/tags")]
    public async Task<ActionResult> UpdateTags(int id, [FromBody] UpdateTagsRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var pastOrder = _pastOrderService.FindFullyIncluded(p => p.Id == id).FirstOrDefault();
            if (pastOrder == null) return NotFound("Past Order not found");

            // Check access: ClientId must match
            if (pastOrder.ClientId != user.ClientId)
                return Forbid();

            // Delete existing tags
            var existingTags = _tagService.Find(t => t.PastOrderId == id).ToList();
            foreach (var tag in existingTags)
            {
                _tagService.Delete(tag);
            }

            // Add new tags
            foreach (var tagName in request.Tags)
            {
                _tagService.Create(new PastOrderTag
                {
                    PastOrderId = id,
                    Tag = tagName
                });
            }

            return Ok();
        }
        catch (Exception ex)
        {
            await _errorLogService.LogErrorAsync(
                "Past Order Error",
                "Failed to Update Tags",
                ex.Message,
                ex,
                additionalData: new { pastOrderId = id },
                userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                requestUrl: HttpContext.Request.Path,
                httpMethod: HttpContext.Request.Method);
            return StatusCode(500, new { message = "Failed to update tags" });
        }
    }

    [HttpPost("{id}/reorder")]
    public async Task<ActionResult<ReorderResultEVM>> Reorder(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var pastOrder = _pastOrderService.FindFullyIncluded(p => p.Id == id).FirstOrDefault();
            if (pastOrder == null) return NotFound("Past Order not found");

            // Check access
            if (pastOrder.ClientId != user.ClientId)
                return Forbid();

            // Get user's cart
            var cart = _cartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
            if (cart == null) return NotFound("Shopping cart not found");

            // Get available contract items for user's client
            var clientContractItemIds = user.ClientId.HasValue
                ? _contractItemService.Find(c => c.ClientId == user.ClientId).Select(c => c.Id).ToHashSet()
                : new HashSet<int>();

            var result = new ReorderResultEVM();

            // Process each item
            var items = _pastOrderItemService
                .FindInclude(i => i.PastOrderId == id, i => i.ContractItem)
                .ToList();

            foreach (var item in items)
            {
                var itemEvm = _itemMapper.MapToEdit(item);

                if (clientContractItemIds.Contains(item.ContractItemId))
                {
                    // Check if item already in cart
                    var existingCartItem = _cartItemService
                        .Find(ci => ci.ShoppingCartId == cart.Id && ci.ContractItemId == item.ContractItemId)
                        .FirstOrDefault();

                    if (existingCartItem != null)
                    {
                        existingCartItem.Quantity += item.Quantity;
                        _cartItemService.Update(existingCartItem);
                    }
                    else
                    {
                        _cartItemService.Create(new ShoppingCartItem
                        {
                            ShoppingCartId = cart.Id,
                            ContractItemId = item.ContractItemId,
                            Quantity = item.Quantity
                        });
                    }

                    itemEvm.IsAvailable = true;
                    result.AddedItems.Add(itemEvm);
                }
                else
                {
                    itemEvm.IsAvailable = false;
                    result.SkippedItems.Add(itemEvm);
                }
            }

            if (result.SkippedItems.Any())
            {
                result.Message = $"Added {result.AddedItems.Count} items to cart. {result.SkippedItems.Count} unavailable items were skipped.";
            }
            else
            {
                result.Message = $"Added {result.AddedItems.Count} items to cart.";
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            await _errorLogService.LogErrorAsync(
                "Past Order Error",
                "Failed to Reorder",
                ex.Message,
                ex,
                additionalData: new { pastOrderId = id },
                userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                requestUrl: HttpContext.Request.Path,
                httpMethod: HttpContext.Request.Method);
            return StatusCode(500, new { message = "Failed to reorder" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<PastOrderEVM>> CreatePastOrder([FromBody] CreatePastOrderRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var pastOrder = new PastOrder
            {
                UserId = userId,
                ClientId = user.ClientId,
                PONumber = request.PONumber,
                OrderedAt = DateTime.UtcNow,
                TotalAmount = request.TotalAmount,
                ItemCount = request.Items.Count
            };

            _pastOrderService.Create(pastOrder);

            // Create items
            foreach (var item in request.Items)
            {
                _pastOrderItemService.Create(new PastOrderItem
                {
                    PastOrderId = pastOrder.Id,
                    ContractItemId = item.ContractItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            // Create tags
            foreach (var tag in request.Tags)
            {
                _tagService.Create(new PastOrderTag
                {
                    PastOrderId = pastOrder.Id,
                    Tag = tag
                });
            }

            return Ok(_mapper.MapToEdit(pastOrder));
        }
        catch (Exception ex)
        {
            await _errorLogService.LogErrorAsync(
                "Past Order Error",
                "Failed to Create Past Order",
                ex.Message,
                ex,
                userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                requestUrl: HttpContext.Request.Path,
                httpMethod: HttpContext.Request.Method);
            return StatusCode(500, new { message = "Failed to create past order" });
        }
    }
}

public class UpdateTagsRequest
{
    public List<string> Tags { get; set; } = new();
}

public class CreatePastOrderRequest
{
    public string? PONumber { get; set; }
    public List<string> Tags { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public List<CreatePastOrderItemRequest> Items { get; set; } = new();
}

public class CreatePastOrderItemRequest
{
    public int ContractItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
