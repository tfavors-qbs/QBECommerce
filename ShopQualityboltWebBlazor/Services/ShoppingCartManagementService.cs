using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using QBExternalWebLibrary.Services.Http;
using static MudBlazor.CategoryTypes;

namespace ShopQualityboltWebBlazor.Services
{
	public class ShoppingCartManagementService(ShoppingCartPageApiService _shoppingCartPageService)
	{
		public ShoppingCartPageEVM UsersShoppingCartEVM { get => field; set { field = value; UsersShoppingCartEVMChanged?.Invoke(value); } }
		public Action<ShoppingCartPageEVM> UsersShoppingCartEVMChanged { get; set; }
		public async Task<ShoppingCartPageEVM> RefreshUserShoppingCart()
		{
			return UsersShoppingCartEVM = await _shoppingCartPageService.GetPageAsync();
		}

		public async Task<ShoppingCartPageEVM> AddItemAsync(ShoppingCartItemEVM item)
		{
			await _shoppingCartPageService.AddItemAsync(item);
			return await RefreshUserShoppingCart();
		}

		public async Task<ShoppingCartPageEVM> UpdateItemAsync(ShoppingCartItemEVM item)
		{
			await _shoppingCartPageService.UpdateItemAsync(item);
			return await RefreshUserShoppingCart();
		}

		public async Task<ShoppingCartPageEVM> DeleteItemAsync (ShoppingCartItemEVM item)
		{
			await _shoppingCartPageService.DeleteItemAsync(item);
			return await RefreshUserShoppingCart();
		}
	}
}
