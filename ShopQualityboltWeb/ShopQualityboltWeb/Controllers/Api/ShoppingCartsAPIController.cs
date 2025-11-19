using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Services.Model;
using QBExternalWebLibrary.Models.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models.Pages;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using ShopQualityboltWeb.Services;

namespace ShopQualityboltWeb.Controllers.Api {


    [Route("api/shoppingcarts")]
    [ApiController]
    [Authorize]
    public class ShoppingCartsAPIController : Controller {
        private readonly IModelService<ShoppingCart, ShoppingCartEVM> _service;
        private readonly IModelMapper<ShoppingCart, ShoppingCartEVM> _mapper;
        private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IModelService<ShoppingCartItem, ShoppingCartItemEVM?> _shoppingcartItemService;
        private readonly IModelService<ContractItem, ContractItemEditViewModel?> _contractItemService;
		private readonly IErrorLogService _errorLogService;
		private readonly ILogger<ShoppingCartsAPIController> _logger;

        public ShoppingCartsAPIController(
			IModelService<ShoppingCart, ShoppingCartEVM> service, 
			IModelMapper<ShoppingCart, ShoppingCartEVM> mapper, 
			UserManager<ApplicationUser> userManager, 
			IModelService<ShoppingCartItem, ShoppingCartItemEVM?> shoppingcartItemService,
            IModelService<ContractItem, ContractItemEditViewModel?> contractItemService, 
			IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper,
			IErrorLogService errorLogService,
			ILogger<ShoppingCartsAPIController> logger) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
            _shoppingcartItemService = shoppingcartItemService;
            _contractItemService = contractItemService;
            _contractItemMapper = contractItemMapper;
			_errorLogService = errorLogService;
			_logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ShoppingCart>> GetShoppingCart(int id) {
			try
			{
				var shoppingCart = _service.GetById(id);

				if (shoppingCart == null) {
					return NotFound();
				}

				return shoppingCart;
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Get Shopping Cart",
					ex.Message,
					ex,
					additionalData: new { cartId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve shopping cart" });
			}
        }

        [HttpGet()]
        [Authorize]
        public async Task<ActionResult<ShoppingCartEVM>> GetShoppingCart() {
			try
			{
				var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null) return NotFound(new { message = "User not found." });
				var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
				if (usersShoppingCart == null) {
					ShoppingCartEVM cart = new ShoppingCartEVM() { ApplicationUserId = user.Id };
					_service.Create(null, cart);
					return CreatedAtAction("GetShoppingCart", new { id = cart.Id }, cart);
				} else {
					return _mapper.MapToEdit(_service.GetAll().Where(cart => cart.ApplicationUserId == user.Id)).First();
				}
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Get User Shopping Cart",
					ex.Message,
					ex,
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve shopping cart" });
			}
        }

        [HttpGet("get-cart-info")]
        [Authorize]
        public async Task<ActionResult<ShoppingCartPageEVM>> GetCartPageInfo() {
			try
			{
				ShoppingCartPageEVM pageInfo = new ShoppingCartPageEVM();
				var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null) return NotFound(new { message = "User not found." });
				var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
				if (usersShoppingCart == null) {
					ShoppingCartEVM cart = new ShoppingCartEVM() { ApplicationUserId = user.Id };
					_service.Create(null, cart);
				}

				return await GetCartPageEVM(user);
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Get Cart Page Info",
					ex.Message,
					ex,
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve cart information" });
			}
        }

        [HttpPost("add-item")]
        public async Task<ActionResult<ShoppingCartPageEVM>> AddShoppingCartItem([FromBody] ShoppingCartItemEVM model) {
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (userId == null)
					return Unauthorized(new { message = "User is not authenticated." });

				var user = await _userManager.FindByIdAsync(userId);
				if (user == null)
					return NotFound(new { message = "User not found." });

				// Ensure user has a shopping cart
				var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
				if (usersShoppingCart == null) {
					ShoppingCartEVM cart = new ShoppingCartEVM { ApplicationUserId = user.Id };
					usersShoppingCart = _service.Create(null, cart);

					// Check creation worked
					if (usersShoppingCart == null)
					{
						await _errorLogService.LogErrorAsync(
							"Shopping Cart Error",
							"Failed to Create Shopping Cart",
							"Could not create shopping cart for user",
							additionalData: new { userId = user.Id },
							userId: user.Id,
							userEmail: user.Email,
							requestUrl: HttpContext.Request.Path,
							httpMethod: HttpContext.Request.Method);
						return NotFound(new { message = "Shopping cart for user could not be found or created" });
					}
				}

				// Validate ContractItem
				var contractItem = _contractItemService.Find(c => c.Id == model.ContractItemId && c.ClientId == user.ClientId).FirstOrDefault();
				if (contractItem == null)
				{
					await _errorLogService.LogErrorAsync(
						"Shopping Cart Error",
						"Invalid Contract Item",
						$"Contract item {model.ContractItemId} not found or not authorized for user",
						additionalData: new { contractItemId = model.ContractItemId, userId = user.Id, clientId = user.ClientId },
						userId: user.Id,
						userEmail: user.Email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method);
					return BadRequest(new { message = "Invalid or unauthorized ContractItem." });
				}

				var usersShoppingCartItems = _shoppingcartItemService.Find(a => a.ShoppingCartId == usersShoppingCart.Id);
				if(usersShoppingCartItems != null)
				{
					bool cartItemForContractIdExists = usersShoppingCartItems.Any(a => a.ContractItemId == model.ContractItemId);
					if (cartItemForContractIdExists)
					{
						return BadRequest(new { message = "Cannot add a new shopping cart item to shopping cart where contract item already exists. Use Update instead." });
					}
				}

				// Create ShoppingCartItem
				var cartItem = new ShoppingCartItem {
					ShoppingCartId = usersShoppingCart.Id,
					ContractItemId = model.ContractItemId,
					Quantity = model.Quantity
				};
				_shoppingcartItemService.Create(cartItem);

				_logger.LogInformation("Added item {ContractItemId} to cart for user {Email}", model.ContractItemId, user.Email);

				return await GetCartPageEVM(user);
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Add Cart Item",
					ex.Message,
					ex,
					additionalData: new { contractItemId = model?.ContractItemId, quantity = model?.Quantity },
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to add item to cart" });
			}
        }

        [HttpPut("items/{id}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> UpdateShoppingCartItem(int id, [FromBody] ShoppingCartItemEVM model) {
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (userId == null)
					return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null)
					return NotFound(new { message = "User not found." });
				var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
				if (usersShoppingCart == null)
					return NotFound(new { message = "User's cart not found." });
				var cartItem = _shoppingcartItemService.Find(i => i.Id == id && i.ShoppingCartId == usersShoppingCart.Id).FirstOrDefault();
				if (cartItem == null)
					return NotFound(new { message = "Cart item not found or not authorized." });
				cartItem.Quantity = model.Quantity;
				_shoppingcartItemService.Update(cartItem);
				
				_logger.LogInformation("Updated cart item {ItemId} quantity to {Quantity} for user {Email}", id, model.Quantity, user.Email);
				
				return await GetCartPageEVM(user);
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Update Cart Item",
					ex.Message,
					ex,
					additionalData: new { cartItemId = id, quantity = model?.Quantity },
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to update cart item" });
			}
        }

        [HttpDelete("items/{id}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> DeleteShoppingCartItem(int id) {
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (userId == null)
					return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null)
					return NotFound(new { message = "User not found." });
				var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
				if (usersShoppingCart == null)
					return NotFound(new { message = "User's cart not found." });
				var cartItem = _shoppingcartItemService.Find(i => i.Id == id && i.ShoppingCartId == usersShoppingCart.Id).FirstOrDefault();
				if (cartItem == null)
					return NotFound(new { message = "Cart item not found or not authorized." });
				_shoppingcartItemService.Delete(cartItem);
				
				_logger.LogInformation("Deleted cart item {ItemId} for user {Email}", id, user.Email);
				
				return await GetCartPageEVM(user);
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Delete Cart Item",
					ex.Message,
					ex,
					additionalData: new { cartItemId = id },
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to delete cart item" });
			}
        }

		[HttpDelete("clear/{cartId}")]
		public async Task<ActionResult<ShoppingCartPageEVM>> ClearShoppingCart(int cartId)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (userId == null)
					return Unauthorized(new { message = "User is not authenticated." });
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null)
					return NotFound(new { message = "User not found." });
				var usersShoppingCart = _service.FindInclude(a => a.ApplicationUserId == user.Id, b => b.ShoppingCartItems).FirstOrDefault();
				if (usersShoppingCart == null)
					return NotFound(new { message = "User's cart not found." });
				_shoppingcartItemService.DeleteRange(usersShoppingCart.ShoppingCartItems);
				
				_logger.LogInformation("Cleared shopping cart {CartId} for user {Email}", cartId, user.Email);
				
				return await GetCartPageEVM(user);
			}
			catch (Exception ex)
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Error",
					"Failed to Clear Shopping Cart",
					ex.Message,
					ex,
					additionalData: new { cartId },
					userId: userId,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to clear shopping cart" });
			}
		}

		#region Helpers
		private async Task<ShoppingCartPageEVM> GetCartPageEVM(ApplicationUser user) {
            var pageInfo = new ShoppingCartPageEVM();
			var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
			if (usersShoppingCart != null)
            {
				pageInfo.ShoppingCartEVM = _mapper.MapToEdit(usersShoppingCart);
				var cartItems = _shoppingcartItemService.Find(i => i.ShoppingCartId.Equals(usersShoppingCart.Id)).ToList();
				var contractItems = _contractItemService.Find(c => c.ClientId == user.ClientId).ToList();
				pageInfo.ShoppingCartItemEVMs = (from cartItem in cartItems
												 join contractItem in contractItems
												 on cartItem.ContractItemId equals contractItem.Id
												 select new ShoppingCartItemEVM
												 {
													 Id = cartItem.Id,
													 ShoppingCartId = cartItem.ShoppingCartId,
													 ContractItemId = cartItem.ContractItemId,
													 ContractItemEditViewModel = _contractItemMapper.MapToEdit(contractItem),
													 Quantity = cartItem.Quantity
												 }).ToDictionary(a => a.ContractItemId);
			}
			
            return pageInfo;
        }
        #endregion
    }
}
