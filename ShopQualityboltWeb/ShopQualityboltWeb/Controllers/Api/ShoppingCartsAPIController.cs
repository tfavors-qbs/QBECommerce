using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Services.Model;
using QBExternalWebLibrary.Models.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Mapping;

namespace ShopQualityboltWeb.Controllers.Api {


    [Route("api/shoppingcarts")]
    [ApiController]
    public class ShoppingCartsAPIController : Controller {
        private readonly IModelService<ShoppingCart, ShoppingCartEVM?> _service;
        private readonly IModelMapper<ShoppingCart, ShoppingCartEVM> _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartsAPIController(IModelService<ShoppingCart, ShoppingCartEVM> service, IModelMapper<ShoppingCart, ShoppingCartEVM> mapper, UserManager<ApplicationUser> userManager) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ShoppingCartEVM>> GetShoppingCart(int id) {
            var shoppingCart = _mapper.MapToEdit(_service.GetById(id));

            if (shoppingCart == null) {
                return NotFound();
            }

            return shoppingCart;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ShoppingCartEVM>> GetShoppingCart() {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized(new { message = "User is not authenticated." });
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });
            if (user.ShoppingCartId == null) {
                ShoppingCartEVM cart = new ShoppingCartEVM() { ApplicationUserId = user.Id };
                _service.Create(null, cart);
                return CreatedAtAction("GetShoppingCart", new { id = cart.Id }, cart);
            } else {
                return _mapper.MapToEdit(_service.GetAll().Where(cart => cart.ApplicationUserId == user.Id)).First();
            }
        }
    }
}
