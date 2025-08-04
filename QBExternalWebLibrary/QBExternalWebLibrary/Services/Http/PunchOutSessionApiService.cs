using Ariba;
using QBExternalWebLibrary.Models.Ariba;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http
{
	public class PunchOutSessionApiService : ApiService<PunchOutSession, PunchOutSession>
	{
		public PunchOutSessionApiService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
		{
		}

		public async Task<PunchOutSession> GetBySessionIdAsync(string id)
		{
			var response = await _httpClient.GetAsync($"{_endpoint}/sessionid/{id}");
			if (!response.IsSuccessStatusCode) return default;
			return await response.Content.ReadFromJsonAsync<PunchOutSession>();
		}
	}
}
