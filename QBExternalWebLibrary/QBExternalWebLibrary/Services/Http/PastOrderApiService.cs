using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using System.Net.Http.Json;

namespace QBExternalWebLibrary.Services.Http;

public class PastOrderApiService
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = "api/past-orders";

    public PastOrderApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Auth");
    }

    public async Task<PastOrderPageEVM?> GetAllAsync()
    {
        var response = await _httpClient.GetAsync(Endpoint).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PastOrderPageEVM>().ConfigureAwait(false);
    }

    public async Task<PastOrderDetailEVM?> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/{id}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PastOrderDetailEVM>().ConfigureAwait(false);
    }

    public async Task<bool> UpdateTagsAsync(int id, List<string> tags)
    {
        var request = new { Tags = tags };
        var response = await _httpClient.PutAsJsonAsync($"{Endpoint}/{id}/tags", request).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<ReorderResultEVM?> ReorderAsync(int id)
    {
        var response = await _httpClient.PostAsync($"{Endpoint}/{id}/reorder", null).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReorderResultEVM>().ConfigureAwait(false);
    }

    public async Task<PastOrderEVM?> CreateAsync(CreatePastOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(Endpoint, request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PastOrderEVM>().ConfigureAwait(false);
    }
}

public class CreatePastOrderRequest
{
    public string? PONumber { get; set; }
    public List<string> Tags { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public List<CreatePastOrderItemRequest> Items { get; set; } = new();
}

public class CreatePastOrderItemRequest
{
    public int ContractItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
