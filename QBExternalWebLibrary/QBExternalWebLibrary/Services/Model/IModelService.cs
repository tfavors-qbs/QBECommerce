using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq.Expressions;

namespace QBExternalWebLibrary.Services.Model
{
    public interface IModelService<TModel, TView>
    {
        TModel? GetById(int? id);
        IEnumerable<TModel> GetAll();
        IEnumerable<TModel> Find(Expression<Func<TModel, bool>> predicate);
		IEnumerable<TModel> FindInclude<TProperty>(Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, TProperty>> includeExpression);
		IEnumerable<TModel> FindFullyIncluded(Expression<Func<TModel, bool>> predicate);
		TModel Create(TModel? entity, TView view = default);
        IEnumerable<TModel> CreateRange(IEnumerable<TModel>? entities, IEnumerable<TView> views = default);
        TModel Update(TModel? entity, TView view = default);
        void Delete(TModel? entity, TView view = default);
		void DeleteRange(IEnumerable<TModel> entity);
		bool Exists(Func<TModel, bool> predicate);
        TView GetView(TModel entity);
        //TModel GetModel(TView entity);
    }
}
