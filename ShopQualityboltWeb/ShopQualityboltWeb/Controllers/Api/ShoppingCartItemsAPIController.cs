using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api {

    [Route("api/shoppingcartitems")]
    [ApiController]
    [Authorize]
    public class ShoppingCartItemsAPIController : Controller {
        private readonly IModelService<ShoppingCartItem, ShoppingCartItemEVM?> _service;
        private readonly IModelMapper<ShoppingCartItem, ShoppingCartItemEVM> _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IErrorLogService _errorLogService;

        public ShoppingCartItemsAPIController(
            IModelService<ShoppingCartItem, ShoppingCartItemEVM> service,
            IModelMapper<ShoppingCartItem, ShoppingCartItemEVM> mapper,
            UserManager<ApplicationUser> userManager,
            IErrorLogService errorLogService) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
            _errorLogService = errorLogService;
        }

		[HttpGet("shoppingcart/{cartId}")]
		public async Task<ActionResult<IEnumerable<ShoppingCartItem>>> GetShoppingCartItemsByShoppingCartId(int cartId)
		{
			try {
				var items = _service.FindFullyIncluded(a => a.ShoppingCartId == cartId);

				if (items == null)
				{
					return NotFound();
				}

				return Ok(items);
			} catch (Exception ex) {
				await _errorLogService.LogErrorAsync(
					"Shopping Cart Items Error",
					"Failed to Get Shopping Cart Items",
					ex.Message,
					ex,
					additionalData: new { cartId },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, new { message = "Failed to retrieve shopping cart items" });
			}
		}
	}
}
