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

namespace ShopQualityboltWeb.Controllers.Api {


    [Route("api/shoppingcarts")]
    [ApiController]
    public class ShoppingCartsAPIController : Controller {
        private readonly IModelService<ShoppingCart, ShoppingCartEVM> _service;
        private readonly IModelMapper<ShoppingCart, ShoppingCartEVM> _mapper;
        private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IModelService<ShoppingCartItem, ShoppingCartItemEVM?> _shoppingcartItemService;
        private readonly IModelService<ContractItem, ContractItemEditViewModel?> _contractItemService;

        public ShoppingCartsAPIController(IModelService<ShoppingCart, ShoppingCartEVM> service, IModelMapper<ShoppingCart, 
            ShoppingCartEVM> mapper, UserManager<ApplicationUser> userManager, IModelService<ShoppingCartItem, ShoppingCartItemEVM?> shoppingcartItemService,
            IModelService<ContractItem, ContractItemEditViewModel?> contractItemService, IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
            _shoppingcartItemService = shoppingcartItemService;
            _contractItemService = contractItemService;
            _contractItemMapper = contractItemMapper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ShoppingCartEVM>> GetShoppingCart(int id) {
            var shoppingCart = _mapper.MapToEdit(_service.GetById(id));

            if (shoppingCart == null) {
                return NotFound();
            }

            return shoppingCart;
        }

        [HttpGet()]
        [Authorize]
        public async Task<ActionResult<ShoppingCartEVM>> GetShoppingCart() {
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

        [HttpGet("get-cart-info")]
        [Authorize]
        public async Task<ActionResult<ShoppingCartPageEVM>> GetCartPageInfo() {
            List<ShoppingCartItem> cartItems;
            List<ContractItem> contractItems;
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

        [HttpPost("add-item")]
        public async Task<ActionResult<ShoppingCartPageEVM>> AddShoppingCartItem([FromBody] ShoppingCartItemEVM model) {
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
                    return NotFound(new { message = "Shopping cart for user could not be found or created" }); //TODO: may need to return a different Result Object than NotFound
				}
			}

            // Validate ContractItem
            var contractItem = _contractItemService.Find(c => c.Id == model.ContractItemId && c.ClientId == user.ClientId).FirstOrDefault();
            if (contractItem == null)
                return BadRequest(new { message = "Invalid or unauthorized ContractItem." });

            var usersShoppingCartItems = _shoppingcartItemService.Find(a => a.ShoppingCartId == usersShoppingCart.Id);
            if(usersShoppingCartItems != null)
            {
                bool cartItemForContractIdExists = usersShoppingCartItems.Count(a => a.ContractItemId == model.ContractItemId) > 0;
				if (cartItemForContractIdExists)
                {
					return BadRequest(new { message = "Cannot add a new contract item to shopping cart where contract item already exists. Use Update instead." });
				}
            }

			// Create ShoppingCartItem
			var cartItem = new ShoppingCartItem {
                ShoppingCartId = usersShoppingCart.Id,
                ContractItemId = model.ContractItemId,
                Quantity = model.Quantity
            };
            _shoppingcartItemService.Create(cartItem);

            return await GetCartPageEVM(user);            
        }

        [HttpPut("items/{id}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> UpdateShoppingCartItem(int id, [FromBody] ShoppingCartItemEVM model) {
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
				return NotFound(new { message = "Users cart not found" });
			var cartItem = _shoppingcartItemService.Find(i => i.Id == id && i.ShoppingCartId == usersShoppingCart.Id).FirstOrDefault();
            if (cartItem == null)
                return NotFound(new { message = "Cart item not found or not authorized." });
            cartItem.Quantity = model.Quantity;
            _shoppingcartItemService.Update(cartItem);
            return await GetCartPageEVM(user);
        }

        [HttpDelete("items/{id}")]
        public async Task<ActionResult<ShoppingCartPageEVM>> DeleteShoppingCartItem(int id) {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User is not authenticated." });
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });
			var usersShoppingCart = _service.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
			if (usersShoppingCart == null)
				return NotFound(new { message = "Users cart not found" });
			var cartItem = _shoppingcartItemService.Find(i => i.Id == id && i.ShoppingCartId == usersShoppingCart.Id).FirstOrDefault();
            if (cartItem == null)
                return NotFound(new { message = "Cart item not found or not authorized." });
            _shoppingcartItemService.Delete(cartItem);
            return await GetCartPageEVM(user);
        }

		[HttpDelete("clear/{cartId}")]
		public async Task<ActionResult<ShoppingCartPageEVM>> ClearShoppingCart(int cartId)
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null)
				return Unauthorized(new { message = "User is not authenticated." });
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
				return NotFound(new { message = "User not found." });
			var usersShoppingCart = _service.FindInclude(a => a.ApplicationUserId == user.Id, b => b.ShoppingCartItems).FirstOrDefault();
			if (usersShoppingCart == null)
				return NotFound(new { message = "Users cart not found" });
			_shoppingcartItemService.DeleteRange(usersShoppingCart.ShoppingCartItems);
			return await GetCartPageEVM(user);
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
