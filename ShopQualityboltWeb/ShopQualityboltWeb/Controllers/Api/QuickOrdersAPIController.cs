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
