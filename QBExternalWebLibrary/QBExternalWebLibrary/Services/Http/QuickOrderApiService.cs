using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using System.Net.Http.Json;

namespace QBExternalWebLibrary.Services.Http;

public class QuickOrderApiService
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = "api/quickorders";

    public QuickOrderApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Auth");
    }

    public async Task<QuickOrderPageEVM?> GetAllAsync()
    {
        var response = await _httpClient.GetAsync(Endpoint).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderPageEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderDetailEVM?> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/{id}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderDetailEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderEVM?> CreateAsync(CreateQuickOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(Endpoint, request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderEVM?> UpdateAsync(int id, UpdateQuickOrderRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"{Endpoint}/{id}", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"{Endpoint}/{id}").ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<QuickOrderEVM?> CopyAsync(int id)
    {
        var response = await _httpClient.PostAsync($"{Endpoint}/{id}/copy", null).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<AddToCartResult?> AddToCartAsync(int id, List<int>? selectedItemIds = null)
    {
        var request = new AddToCartRequest { SelectedItemIds = selectedItemIds };
        var response = await _httpClient.PostAsJsonAsync($"{Endpoint}/{id}/add-to-cart", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AddToCartResult>().ConfigureAwait(false);
    }

    public async Task<List<string>> GetTagsAsync()
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/tags").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return new List<string>();
        return await response.Content.ReadFromJsonAsync<List<string>>().ConfigureAwait(false) ?? new List<string>();
    }

    public async Task<QuickOrderItemEVM?> AddItemAsync(int quickOrderId, int contractItemId, int quantity)
    {
        var request = new QuickOrderItemRequest { ContractItemId = contractItemId, Quantity = quantity };
        var response = await _httpClient.PostAsJsonAsync($"{Endpoint}/{quickOrderId}/items", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderItemEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderItemEVM?> UpdateItemAsync(int quickOrderId, int itemId, int quantity)
    {
        var request = new QuickOrderItemRequest { Quantity = quantity };
        var response = await _httpClient.PutAsJsonAsync($"{Endpoint}/{quickOrderId}/items/{itemId}", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderItemEVM>().ConfigureAwait(false);
    }

    public async Task<bool> RemoveItemAsync(int quickOrderId, int itemId)
    {
        var response = await _httpClient.DeleteAsync($"{Endpoint}/{quickOrderId}/items/{itemId}").ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}

public class CreateQuickOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
    public List<QuickOrderItemRequest>? Items { get; set; }
}

public class UpdateQuickOrderRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
}

public class QuickOrderItemRequest
{
    public int ContractItemId { get; set; }
    public int Quantity { get; set; }
}

public class AddToCartRequest
{
    public List<int>? SelectedItemIds { get; set; }
}

public class AddToCartResult
{
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
