using Ariba;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http
{
	public class ShoppingCartPageApiService : ApiService<ShoppingCart, ShoppingCartEVM>
	{
		public ShoppingCartPageApiService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public async Task<ShoppingCartPageEVM> GetPageAsync()
		{
			var response = await _httpClient.GetAsync($"{_endpoint}/get-cart-info").ConfigureAwait(false);
			if (!response.IsSuccessStatusCode) return default;
			return await response.Content.ReadFromJsonAsync<ShoppingCartPageEVM>().ConfigureAwait(false);
		}

		public async Task<ShoppingCartPageEVM> AddItemAsync(ShoppingCartItemEVM model)
		{
			var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/add-item", model).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode) return default;
			return await response.Content.ReadFromJsonAsync<ShoppingCartPageEVM>().ConfigureAwait(false);
		}

		public async Task<ShoppingCartPageEVM> UpdateItemAsync(ShoppingCartItemEVM model)
		{
			var response = await _httpClient.PutAsJsonAsync($"{_endpoint}/items/{model.Id}", model).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode) return default;
			return await response.Content.ReadFromJsonAsync<ShoppingCartPageEVM>().ConfigureAwait(false);
		}

		public async Task<ShoppingCartPageEVM> DeleteItemAsync(ShoppingCartItemEVM model)
		{
			var response = await _httpClient.DeleteAsync($"{_endpoint}/items/{model.Id}").ConfigureAwait(false);
			if (!response.IsSuccessStatusCode) return default;
			return await response.Content.ReadFromJsonAsync<ShoppingCartPageEVM>().ConfigureAwait(false);
		}
	}
}
