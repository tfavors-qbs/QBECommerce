using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models.Pages;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/qbsales/carts")]
    [ApiController]
    [Authorize(Roles = "QBSales,Admin")]
    public class QBSalesCartController : ControllerBase
    {
        private readonly IModelService<ShoppingCart, ShoppingCartEVM> _shoppingCartService;
        private readonly IModelService<ShoppingCartItem, ShoppingCartItemEVM?> _shoppingCartItemService;
        private readonly IModelService<ContractItem, ContractItemEditViewModel?> _contractItemService;
        private readonly IModelMapper<ShoppingCart, ShoppingCartEVM> _shoppingCartMapper;
        private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DataContext _context;
        private readonly ILogger<QBSalesCartController> _logger;

        public QBSalesCartController(
            IModelService<ShoppingCart, ShoppingCartEVM> shoppingCartService,
            IModelService<ShoppingCartItem, ShoppingCartItemEVM?> shoppingCartItemService,
            IModelService<ContractItem, ContractItemEditViewModel?> contractItemService,
            IModelMapper<ShoppingCart, ShoppingCartEVM> shoppingCartMapper,
            IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper,
            UserManager<ApplicationUser> userManager,
            DataContext context,
            ILogger<QBSalesCartController> logger)
        {
            _shoppingCartService = shoppingCartService;
            _shoppingCartItemService = shoppingCartItemService;
            _contractItemService = contractItemService;
            _shoppingCartMapper = shoppingCartMapper;
            _contractItemMapper = contractItemMapper;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all shopping carts with client information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShoppingCartWithUserInfo>>> GetAllCarts()
        {
            try
            {
                // Materialize the carts collection first to avoid multiple active result sets error
                var carts = _shoppingCartService.GetAll().ToList();
                var cartsWithInfo = new List<ShoppingCartWithUserInfo>();

                // Get all user IDs from carts
                var userIds = carts.Select(c => c.ApplicationUserId).Distinct().ToList();
                
                // Load all users with their Client data in one query
                var users = await _context.Users
                    .Include(u => u.Client)
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var cart in carts)
                {
                    var user = users.FirstOrDefault(u => u.Id == cart.ApplicationUserId);
                    if (user != null)
                    {
                        var cartItems = _shoppingCartItemService.Find(item => item.ShoppingCartId == cart.Id).ToList();
                        
                        cartsWithInfo.Add(new ShoppingCartWithUserInfo
                        {
                            CartId = cart.Id,
                            UserId = user.Id,
                            UserEmail = user.Email ?? "",
                            UserName = $"{user.GivenName ?? ""} {user.FamilyName ?? ""}".Trim(),
                            ClientId = user.ClientId,
                            ClientName = user.Client?.Name ?? "",
                            ItemCount = cartItems.Count,
                            TotalQuantity = cartItems.Sum(i => i.Quantity),
                            LastModified = cart.ShoppingCartItems?.Any() == true 
                                ? cart.ShoppingCartItems.Max(i => i.Id) 
                                : 0
                        });
                    }
                }

                return Ok(cartsWithInfo.OrderByDescending(c => c.LastModified));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shopping carts");
                return StatusCode(500, "Internal server error while retrieving carts");
            }
        }

        /// <summary>
        /// Get shopping cart for a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> GetUserCart(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound($"User with ID {userId} not found");

                var cart = _shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
                if (cart == null)
                {
                    // Create cart if it doesn't exist
                    var newCart = new ShoppingCartEVM { ApplicationUserId = userId };
                    cart = _shoppingCartService.Create(null, newCart);
                }

                return Ok(await GetCartPageInfo(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
                return StatusCode(500, "Internal server error while retrieving user cart");
            }
        }

        /// <summary>
        /// Get shopping carts filtered by client
        /// </summary>
        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<IEnumerable<ShoppingCartWithUserInfo>>> GetCartsByClient(int clientId)
        {
            try
            {
                // Load users with their Client data
                var users = await _context.Users
                    .Include(u => u.Client)
                    .Where(u => u.ClientId == clientId)
                    .ToListAsync();
                
                var cartsWithInfo = new List<ShoppingCartWithUserInfo>();

                foreach (var user in users)
                {
                    var cart = _shoppingCartService.Find(c => c.ApplicationUserId == user.Id).FirstOrDefault();
                    if (cart != null)
                    {
                        var cartItems = _shoppingCartItemService.Find(item => item.ShoppingCartId == cart.Id).ToList();
                        
                        cartsWithInfo.Add(new ShoppingCartWithUserInfo
                        {
                            CartId = cart.Id,
                            UserId = user.Id,
                            UserEmail = user.Email,
                            UserName = $"{user.GivenName ?? ""} {user.FamilyName ?? ""}".Trim(),
                            ClientId = user.ClientId,
                            ClientName = user.Client?.Name,
                            ItemCount = cartItems.Count,
                            TotalQuantity = cartItems.Sum(i => i.Quantity)
                        });
                    }
                }

                return Ok(cartsWithInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving carts for client {ClientId}", clientId);
                return StatusCode(500, "Internal server error while retrieving client carts");
            }
        }

        /// <summary>
        /// Add item to a user's shopping cart
        /// </summary>
        [HttpPost("user/{userId}/items")]
        public async Task<ActionResult<ShoppingCartPageEVM>> AddItemToUserCart(string userId, [FromBody] AddCartItemRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound($"User with ID {userId} not found");

                // Validate the contract item exists using direct database query
                var contractItem = await _context.ContractItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ci => ci.Id == request.ContractItemId);
                    
                if (contractItem == null)
                {
                    _logger.LogWarning("Attempted to add non-existent contract item {ContractItemId} to cart for user {UserId}", 
                        request.ContractItemId, userId);
                    return BadRequest($"Contract item with ID {request.ContractItemId} not found");
                }

                // Validate the contract item belongs to the user's client
                if (user.ClientId.HasValue && contractItem.ClientId != user.ClientId.Value)
                {
                    _logger.LogWarning("Attempted to add contract item {ContractItemId} from client {ItemClientId} to cart for user {UserId} in client {UserClientId}", 
                        request.ContractItemId, contractItem.ClientId, userId, user.ClientId.Value);
                    return BadRequest($"Contract item does not belong to user's client");
                }

                var cart = _shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
                if (cart == null)
                {
                    var newCart = new ShoppingCartEVM { ApplicationUserId = userId };
                    cart = _shoppingCartService.Create(null, newCart);
                }

                // Check if item already exists in cart
                var existingItem = _shoppingCartItemService.Find(i => 
                    i.ShoppingCartId == cart.Id && i.ContractItemId == request.ContractItemId).FirstOrDefault();

                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += request.Quantity;
                    _shoppingCartItemService.Update(existingItem, null);
                }
                else
                {
                    // CRITICAL FIX: Verify ContractItem exists in THIS context before inserting
                    // This ensures the FK constraint can be satisfied
                    var contractItemInContext = await _context.ContractItems
                        .FirstOrDefaultAsync(ci => ci.Id == request.ContractItemId);
                    
                    if (contractItemInContext == null)
                    {
                        _logger.LogError("Contract item {ContractItemId} existed during validation but not during insert for user {UserId}", 
                            request.ContractItemId, userId);
                        return BadRequest($"Contract item with ID {request.ContractItemId} is no longer available");
                    }

                    // Add new item
                    var newItem = new ShoppingCartItemEVM
                    {
                        ShoppingCartId = cart.Id,
                        ContractItemId = request.ContractItemId,
                        Quantity = request.Quantity
                    };
                    _shoppingCartItemService.Create(null, newItem);
                }

                _logger.LogInformation("QBSales user {SalesUser} added item {ContractItemId} (Qty: {Quantity}) to cart for user {TargetUser}", 
                    User.Identity?.Name, request.ContractItemId, request.Quantity, userId);

                return Ok(await GetCartPageInfo(user));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FK_ShoppingCartItems_ContractItems") == true)
            {
                _logger.LogError(ex, "FK constraint violation adding contract item {ContractItemId} to cart for user {UserId}. Contract item may have been deleted.", 
                    request.ContractItemId, userId);
                return BadRequest($"Contract item with ID {request.ContractItemId} is no longer available");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}. ContractItemId: {ContractItemId}", 
                    userId, request.ContractItemId);
                return StatusCode(500, $"Internal server error while adding item to cart: {ex.Message}");
            }
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPut("user/{userId}/items/{itemId}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> UpdateCartItem(string userId, int itemId, [FromBody] UpdateCartItemRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound($"User with ID {userId} not found");

                var item = _shoppingCartItemService.GetById(itemId);
                if (item == null)
                    return NotFound($"Cart item with ID {itemId} not found");

                // Verify the item belongs to the user's cart
                var cart = _shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
                if (cart == null || item.ShoppingCartId != cart.Id)
                    return BadRequest("Cart item does not belong to this user");

                item.Quantity = request.Quantity;
                _shoppingCartItemService.Update(item, null);

                _logger.LogInformation("QBSales user {SalesUser} updated item {ItemId} quantity to {Quantity} for user {TargetUser}", 
                    User.Identity?.Name, itemId, request.Quantity, userId);

                return Ok(await GetCartPageInfo(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {ItemId} for user {UserId}", itemId, userId);
                return StatusCode(500, "Internal server error while updating cart item");
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("user/{userId}/items/{itemId}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> RemoveCartItem(string userId, int itemId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound($"User with ID {userId} not found");

                var item = _shoppingCartItemService.GetById(itemId);
                if (item == null)
                    return NotFound($"Cart item with ID {itemId} not found");

                // Verify the item belongs to the user's cart
                var cart = _shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
                if (cart == null || item.ShoppingCartId != cart.Id)
                    return BadRequest("Cart item does not belong to this user");

                _shoppingCartItemService.Delete(item);

                _logger.LogInformation("QBSales user {SalesUser} removed item {ItemId} from cart for user {TargetUser}", 
                    User.Identity?.Name, itemId, userId);

                return Ok(await GetCartPageInfo(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {ItemId} for user {UserId}", itemId, userId);
                return StatusCode(500, "Internal server error while removing cart item");
            }
        }

        /// <summary>
        /// Clear all items from a user's cart
        /// </summary>
        [HttpDelete("user/{userId}/clear")]
        public async Task<ActionResult<ShoppingCartPageEVM>> ClearUserCart(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound($"User with ID {userId} not found");

                var cart = _shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
                if (cart == null)
                    return NotFound("Cart not found for user");

                var items = _shoppingCartItemService.Find(i => i.ShoppingCartId == cart.Id).ToList();
                foreach (var item in items)
                {
                    _shoppingCartItemService.Delete(item);
                }

                _logger.LogInformation("QBSales user {SalesUser} cleared cart for user {TargetUser}", 
                    User.Identity?.Name, userId);

                return Ok(await GetCartPageInfo(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return StatusCode(500, "Internal server error while clearing cart");
            }
        }

        /// <summary>
        /// Get all users for a specific client (for creating new carts)
        /// </summary>
        [HttpGet("client/{clientId}/users")]
        public async Task<ActionResult<IEnumerable<UserCartInfo>>> GetClientUsers(int clientId)
        {
            try
            {
                var users = _userManager.Users.Where(u => u.ClientId == clientId).ToList();
                var userInfoList = new List<UserCartInfo>();

                foreach (var user in users)
                {
                    var cart = _shoppingCartService.Find(c => c.ApplicationUserId == user.Id).FirstOrDefault();
                    
                    userInfoList.Add(new UserCartInfo
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        UserName = $"{user.GivenName ?? ""} {user.FamilyName ?? ""}".Trim(),
                        HasCart = cart != null,
                        CartId = cart?.Id
                    });
                }

                return Ok(userInfoList.OrderBy(u => u.UserName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for client {ClientId}", clientId);
                return StatusCode(500, "Internal server error while retrieving users");
            }
        }

        /// <summary>
        /// Create a new shopping cart for a user
        /// </summary>
        [HttpPost("user/{userId}/create")]
        public async Task<ActionResult<ShoppingCartPageEVM>> CreateCartForUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound($"User with ID {userId} not found");

                // Check if cart already exists
                var existingCart = _shoppingCartService.Find(c => c.ApplicationUserId == userId).FirstOrDefault();
                if (existingCart != null)
                    return Conflict(new { message = "Cart already exists for this user", cartId = existingCart.Id });

                // Create new cart
                var newCart = new ShoppingCartEVM { ApplicationUserId = userId };
                var createdCart = _shoppingCartService.Create(null, newCart);

                _logger.LogInformation("QBSales user {SalesUser} created cart for user {TargetUser}", 
                    User.Identity?.Name, userId);

                return Ok(await GetCartPageInfo(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart for user {UserId}", userId);
                return StatusCode(500, "Internal server error while creating cart");
            }
        }

        private async Task<ShoppingCartPageEVM> GetCartPageInfo(ApplicationUser user)
        {
            var cart = _shoppingCartService.Find(c => c.ApplicationUserId == user.Id).FirstOrDefault();
            if (cart == null)
            {
                return new ShoppingCartPageEVM
                {
                    ShoppingCartEVM = new ShoppingCartEVM { ApplicationUserId = user.Id },
                    ShoppingCartItemEVMs = new Dictionary<int, ShoppingCartItemEVM>()
                };
            }

            var cartItems = _shoppingCartItemService.FindFullyIncluded(i => i.ShoppingCartId == cart.Id).ToList();
            var itemDictionary = new Dictionary<int, ShoppingCartItemEVM>();

            foreach (var item in cartItems)
            {
                var contractItem = _contractItemService.GetById(item.ContractItemId);
                if (contractItem != null)
                {
                    var contractItemEVM = _contractItemMapper.MapToEdit(contractItem);
                    itemDictionary[item.ContractItemId] = new ShoppingCartItemEVM
                    {
                        Id = item.Id,
                        ShoppingCartId = item.ShoppingCartId,
                        ContractItemId = item.ContractItemId,
                        Quantity = item.Quantity,
                        ContractItemEditViewModel = contractItemEVM
                    };
                }
            }

            return new ShoppingCartPageEVM
            {
                ShoppingCartEVM = _shoppingCartMapper.MapToEdit(cart),
                ShoppingCartItemEVMs = itemDictionary
            };
        }
    }

    // DTOs for request/response
    public class ShoppingCartWithUserInfo
    {
        public int CartId { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public int ItemCount { get; set; }
        public int TotalQuantity { get; set; }
        public int LastModified { get; set; }
    }

    public class UserCartInfo
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public bool HasCart { get; set; }
        public int? CartId { get; set; }
    }

    public class AddCartItemRequest
    {
        public int ContractItemId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }
}
