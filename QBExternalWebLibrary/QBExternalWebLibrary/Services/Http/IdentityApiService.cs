using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;

namespace QBExternalWebLibrary.Services.Http
{
    public class IdentityApiService : IAuthenticationApiService {
        private readonly HttpClient _httpClient;
        private readonly string _apiHttpBase = "https://localhost:7237/";
        private string _endpoint = "";


        public IdentityApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task RegisterAsync(string email, string password, string givenName, string familyName)
        {
            var loginModel = new
            {
                email,
                password,
                givenName,
                familyName
            };
            HttpResponseMessage response = null;
            response = await _httpClient.PostAsJsonAsync("register", loginModel);
        }

        public async Task<HttpResponseMessage> LoginAsync(string email, string password)
        {
            var loginRequest = new CustomLoginRequest { email = email, password = password };
            HttpResponseMessage response = null;
            response = await _httpClient.PostAsJsonAsync("login", loginRequest);
            return response;
        }

    }
}
