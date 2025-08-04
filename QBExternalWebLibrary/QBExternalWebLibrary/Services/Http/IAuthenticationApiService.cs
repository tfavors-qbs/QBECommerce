using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;

namespace QBExternalWebLibrary.Services.Http
{
    public interface IAuthenticationApiService
    {
        Task RegisterAsync(string email, string password, string givenName, string familyName);
        Task<HttpResponseMessage> LoginAsync(string email, string password);
		Task<HttpResponseMessage> LoginAsync(string sessionId);
	}
}
