using QBExternalWebLibrary.Services.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;
using System.Security.Claims;
using System.Text.Json;

namespace QBExternalWebLibrary.Services.Authentication
{

    public interface IAuthenticationService {
        Task<bool> LoginAsync(string username, string password);
    }
    public class AuthService {
        //private IAuthenticationApiService _authenticationApiService;
        //private readonly LocalStorageService _storageService;
        //private readonly AuthenticationStateProvider _authenticationStateProvider;

        //public AuthService(IAuthenticationApiService authenticationApiService, LocalStorageService storageService, AuthenticationStateProvider authenticationStateProvider) {
        //    _authenticationApiService = authenticationApiService;
        //    _storageService = storageService;
        //    _authenticationStateProvider = authenticationStateProvider;
        //}

        //public async Task<AuthenticationState> LoginAsync(string email, string password) {
        //    HttpResponseMessage response = await _authenticationApiService.LoginAsync(email, password);

        //    if (response.IsSuccessStatusCode) {
        //        var token = await response.Content.ReadFromJsonAsync<LoginResult>();
        //        await _storageService.SetItemAsync("authToken", token.accessToken);
        //        if (string.IsNullOrWhiteSpace(token.accessToken)) {
        //            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        //        }
        //        var claims = ParseClaimsFromToken(token.accessToken);
        //        var identity = new ClaimsIdentity(claims, "jwt");
        //        return new AuthenticationState(new ClaimsPrincipal(identity));
        //    } else {
        //        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        //    }
        //}

        //public async Task LogoutAsync() {
        //    await _storageService.RemoveItemAsync("authToken");
        //}

        //public async Task<string> GetTokenAsync() {
        //    return await _storageService.GeItemAsync("authToken");
        //}

        //private IEnumerable<Claim> ParseClaimsFromToken(string token) {
        //    var payload = token.Split('.')[1];
        //    var jsonBytes = ParseBase64WithoutPadding(payload);
        //    var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        //    return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));

        //}

        //private byte[] ParseBase64WithoutPadding(string base64) {
        //    switch (base64.Length % 4) {
        //        case 2: base64 += "=="; break;
        //        case 3: base64 += "="; break;
        //    }
        //    return Convert.FromBase64String(base64);
        //}
    }
}
