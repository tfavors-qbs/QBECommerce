using Ariba;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http
{
	public class ShoppingCartItemApiService : ApiService<ShoppingCartItem, ShoppingCartItemEVM>
	{
		public ShoppingCartItemApiService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public async Task<IEnumerable<ShoppingCartItem>> GetShoppingCartItemsForShoppingCart(int cartId)
		{
			var response = await _httpClient.GetAsync($"{_endpoint}/shoppingCart/{cartId}").ConfigureAwait(false);
			if (!response.IsSuccessStatusCode) return default;
			return await response.Content.ReadFromJsonAsync<IEnumerable<ShoppingCartItem>>().ConfigureAwait(false);
		}
	}
}
