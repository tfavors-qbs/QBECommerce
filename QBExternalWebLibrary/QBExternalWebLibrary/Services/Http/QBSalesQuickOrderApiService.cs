using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Pages;
using QBExternalWebLibrary.Models.Products;
using System.Net.Http.Json;

namespace QBExternalWebLibrary.Services.Http;

public class QBSalesQuickOrderApiService
{
    private readonly HttpClient _httpClient;
    private const string Endpoint = "api/qbsales/quickorders";
    private const string ContractItemsEndpoint = "api/contractitems";

    public QBSalesQuickOrderApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Auth");
    }

    public async Task<QuickOrderDetailEVM?> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/{id}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderDetailEVM>().ConfigureAwait(false);
    }

    public async Task<List<UserQuickOrderInfo>?> GetUsersByClientAsync(int clientId)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/users/client/{clientId}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<UserQuickOrderInfo>>().ConfigureAwait(false);
    }

    public async Task<List<QuickOrderEVM>?> GetUserQuickOrdersAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/user/{userId}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<QuickOrderEVM>>().ConfigureAwait(false);
    }

    public async Task<List<QuickOrderEVM>?> GetUserDeletedQuickOrdersAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"{Endpoint}/user/{userId}/deleted").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<QuickOrderEVM>>().ConfigureAwait(false);
    }

    public async Task<QuickOrderEVM?> CreateQuickOrderForUserAsync(string userId, CreateQuickOrderForUserRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{Endpoint}/user/{userId}", request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<QuickOrderEVM?> RestoreQuickOrderAsync(int quickOrderId)
    {
        var response = await _httpClient.PostAsync($"{Endpoint}/{quickOrderId}/restore", null).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<QuickOrderEVM>().ConfigureAwait(false);
    }

    public async Task<IEnumerable<ContractItemEditViewModel>?> GetContractItemsByClientAsync(int clientId)
    {
        var response = await _httpClient.GetAsync($"{ContractItemsEndpoint}/client/{clientId}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<IEnumerable<ContractItemEditViewModel>>().ConfigureAwait(false);
    }
}

public class UserQuickOrderInfo
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int QuickOrderCount { get; set; }
    public int DeletedQuickOrderCount { get; set; }
}

public class CreateQuickOrderForUserRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsSharedClientWide { get; set; }
    public List<QuickOrderItemRequest>? Items { get; set; }
}
