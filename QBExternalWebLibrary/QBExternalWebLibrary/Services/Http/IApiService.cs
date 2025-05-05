using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http
{
    public interface IApiService<TEntity, TModel>
    {
        Task<IEnumerable<TModel>> GetAllAsync();
        Task<TEntity> GetByIdAsync(int id);
        Task<bool> CreateAsync(TEntity entity, TModel? model);
        Task<bool> CreateRangeAsync(IEnumerable<TEntity> entities, IEnumerable<TModel>? models = default);
        Task UpdateAsync(int id, TEntity entity, TModel? model);
        Task DeleteAsync(int id);
    }
}
