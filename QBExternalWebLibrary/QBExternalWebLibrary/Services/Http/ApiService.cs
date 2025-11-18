using System.Net.Http.Json;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Products;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using QBExternalWebLibrary.Models.Catalog;
using QBExternalWebLibrary.Models.Ariba;

namespace QBExternalWebLibrary.Services.Http
{
    public class ApiService<TEntity, TModel> : IApiService<TEntity, TModel>
    {
        protected readonly HttpClient _httpClient;
        private readonly Dictionary<Type, string> _endpointMappings = new Dictionary<Type, string>() {
            {typeof(Class), "api/classes" },
            {typeof(Client), "api/clients" },
            {typeof(Coating), "api/coatings" },
            {typeof(ContractItem), "api/contractitems" },
            {typeof(Diameter), "api/diameters" },
            {typeof(Group), "api/groups" },
            {typeof(Length), "api/lengths" },
            {typeof(Material), "api/materials" },
            {typeof(ProductID), "api/productids" },
            {typeof(Shape), "api/shapes" },
            {typeof(SKU), "api/skus" },
            {typeof(Spec), "api/specs" },
            {typeof(Thread), "api/threads" },
			{typeof(ShoppingCart), "api/shoppingcarts" },
			{typeof(ShoppingCartItem), "api/shoppingcartitems" },
			{typeof(PunchOutSession), "api/punchoutsessions" },
		};
        protected string _endpoint = "";

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Auth");
            _endpoint = GetEndpoint();
        }

        /// <summary>
        /// API call to create an entity.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <param name="model">The entity to create based on its edit view model. Most likely required when an entity has any reference type properties.</param>
        /// <returns></returns>
        public async Task<bool> CreateAsync(TEntity entity, TModel? model)
        {
            HttpResponseMessage response = null;
            if (model != null) response = await _httpClient.PostAsJsonAsync(_endpoint, model).ConfigureAwait(false);
            else response = await _httpClient.PostAsJsonAsync(_endpoint, entity).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> CreateRangeAsync(IEnumerable<TEntity> entities, IEnumerable<TModel>? models = default) {
            HttpResponseMessage response = null;
            if (models != null) response = await _httpClient.PostAsJsonAsync($"{_endpoint}/range", models).ConfigureAwait(false);
            else response = await _httpClient.PostAsJsonAsync($"{_endpoint}/range", entities).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Delete entity based on given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_endpoint}/{id}");
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException(response.StatusCode.ToString());
        }

        /// <summary>
        /// Get all entities of given type.
        /// </summary>
        /// <returns>All entites of given type.</returns>
        public async Task<IEnumerable<TModel>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_endpoint).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<TModel>();
            var result = await response.Content.ReadFromJsonAsync<List<TModel>>().ConfigureAwait(false);
            return result ?? Enumerable.Empty<TModel>();
        }

        /// <summary>
        /// Get entity based on given id.
        /// </summary>
        /// <param name="id">Id for needed entity.</param>
        /// <returns>Entity represented by the given id.</returns>
        public async Task<TEntity> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/{id}");
            if (!response.IsSuccessStatusCode) return default;
            var result = await response.Content.ReadFromJsonAsync<TEntity>();
            return result;
        }

        /// <summary>
        /// HttpClient call to api to update(put) an entity
        /// </summary>
        /// <param name="id">Id for the entity to update.</param>
        /// <param name="entity">Entity to update. If using a model, you may leave this parameter null.</param>
        /// <param name="model">Model of the entity to update. Usually needed for entities with reference types as its properties.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task UpdateAsync(int id, TEntity entity, TModel? model)
        {
            HttpResponseMessage response = null;
            if (model != null) response = await _httpClient.PutAsJsonAsync($"{_endpoint}/{id}", model);
            else response = await _httpClient.PutAsJsonAsync($"{_endpoint}/{id}", entity);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Update failed. {response.StatusCode.ToString()}");

        }

        /// <summary>
        /// Gets the endpoint of a given type.
        /// </summary>
        /// <returns>Endpoint string of a given type.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private string GetEndpoint()
        {
            if (!_endpointMappings.TryGetValue(typeof(TEntity), out string endpoint))
            {
                throw new InvalidOperationException($"Endpoint mapping not found for type '{typeof(TEntity).Name}'");
            }
            return endpoint;
        }
    }
}
