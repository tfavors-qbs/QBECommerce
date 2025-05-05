using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QBExternalWebLibrary.Data.Repositories;
using System.Linq.Expressions;
using QBExternalWebLibrary.Models.Mapping;

namespace QBExternalWebLibrary.Services.Model {
    public class ModelService<TModel, TView> : IModelService<TModel, TView> where TModel : class {

        private readonly IRepository<TModel> _repository;
        private readonly IModelMapper<TModel, TView> _mapper;

        public ModelService(IRepository<TModel> repository, IModelMapper<TModel, TView> mapper) {
            _repository = repository;
            _mapper = mapper;
        }
        public TModel Create(TModel? entity, TView view = default) {
            if (entity == null) entity = _mapper.MapToModel(view);
            _repository.Add(entity);
            return entity;
        }
        public IEnumerable<TModel> CreateRange(IEnumerable<TModel>? entities, IEnumerable<TView> views = default) {
            if (entities == null) {
                List<TModel> newEntities = new List<TModel>();
                foreach (TView view in views) {
                    var entity = _mapper.MapToModel(view);
                    newEntities.Add(entity);
                }
                entities = newEntities;
            }
            foreach (TModel entity in entities) {
                _repository.Add(entity);
            }
            return entities;
        }

    public void Delete(TModel entity, TView view = default) {
        if (entity == null) entity = _mapper.MapToModel(view);
        _repository.Remove(entity);
    }

    public IEnumerable<TModel> Find(Expression<Func<TModel, bool>> predicate) {
        return _repository.Find(predicate);
    }

    public IEnumerable<TModel> GetAll() {
        return _repository.GetAll();
    }

    public TModel? GetById(int? id) {
        if (id == null) return null;
        else return _repository.GetById(id);
    }

    public TModel Update(TModel entity, TView view = default) {
        if (entity == null) entity = _mapper.MapToModel(view);
        _repository.Update(entity);
        return entity;
    }

    public bool Exists(Func<TModel, bool> predicate) {
        return _repository.Exists(predicate);
    }

    public TModel Create(TView view) {
        var entity = _mapper.MapToModel(view);
        _repository.Add(entity);
        return entity;
    }

    public TModel Update(TView view) {
        var entity = _mapper.MapToModel(view);
        _repository.Update(entity);
        return entity;
    }

    public TView GetView(TModel entity) {
        return _mapper.MapToEdit(entity);
    }

    //public TModel GetModel(TView entity) {
    //    return _mapper.MapToModel(entity);
    //}
}
}
