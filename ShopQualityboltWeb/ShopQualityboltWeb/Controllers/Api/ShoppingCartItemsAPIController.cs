using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Api {

    [Route("api/shoppingcartitems")]
    [ApiController]
    public class ShoppingCartItemsAPIController : Controller {
        private readonly IModelService<ShoppingCartItem, ShoppingCartItemEVM?> _service;
        private readonly IModelMapper<ShoppingCartItem, ShoppingCartItemEVM> _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartItemsAPIController(IModelService<ShoppingCartItem, ShoppingCartItemEVM> service, IModelMapper<ShoppingCartItem, ShoppingCartItemEVM> mapper, UserManager<ApplicationUser> userManager) {
            _service = service;
            _mapper = mapper;
            _userManager = userManager;
        }
    }
}
