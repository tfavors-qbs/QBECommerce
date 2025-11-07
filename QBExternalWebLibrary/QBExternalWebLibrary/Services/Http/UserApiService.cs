using System.Net.Http.Json;

namespace QBExternalWebLibrary.Services.Http
{
    public interface IUserApiService
    {
        Task<List<UserViewModel>> GetAllUsersAsync();
        Task<UserViewModel> GetUserByIdAsync(string id);
        Task<List<string>> GetRolesAsync();
        Task<UserViewModel> CreateUserAsync(CreateUserRequest request);
        Task<UserViewModel> UpdateUserAsync(string id, UpdateUserRequest request);
        Task DeleteUserAsync(string id);
    }

    public class UserApiService : IUserApiService
    {
        private readonly HttpClient _httpClient;
        private const string Endpoint = "api/users";

        public UserApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Auth");
        }

        public async Task<List<UserViewModel>> GetAllUsersAsync()
        {
            var response = await _httpClient.GetAsync(Endpoint);
            if (!response.IsSuccessStatusCode) return new List<UserViewModel>();
            return await response.Content.ReadFromJsonAsync<List<UserViewModel>>() ?? new List<UserViewModel>();
        }

        public async Task<UserViewModel> GetUserByIdAsync(string id)
        {
            var response = await _httpClient.GetAsync($"{Endpoint}/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserViewModel>();
        }

        public async Task<List<string>> GetRolesAsync()
        {
            var response = await _httpClient.GetAsync($"{Endpoint}/roles");
            if (!response.IsSuccessStatusCode) return new List<string>();
            return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        }

        public async Task<UserViewModel> CreateUserAsync(CreateUserRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(Endpoint, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserViewModel>();
        }

        public async Task<UserViewModel> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"{Endpoint}/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserViewModel>();
        }

        public async Task DeleteUserAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{Endpoint}/{id}");
            response.EnsureSuccessStatusCode();
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string AribaId { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public bool IsDisabled { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string AribaId { get; set; }
        public int? ClientId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string? Password { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string AribaId { get; set; }
        public int? ClientId { get; set; }
        public bool IsDisabled { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
