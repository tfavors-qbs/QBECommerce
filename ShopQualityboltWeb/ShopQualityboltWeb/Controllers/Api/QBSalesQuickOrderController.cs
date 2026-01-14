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
