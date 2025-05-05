using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Linq.Expressions;

namespace QBExternalWebLibrary.Data.Repositories {
    public class EFRepository<TEntity> : IRepository<TEntity> where TEntity : class {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public EFRepository(DataContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
        }
        public virtual TEntity GetById(int? id) {
            return _dbSet.Find(id);
        }

        public virtual IEnumerable<TEntity> GetAll() {
            return _dbSet.ToList();
        }

        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate) {
            return _dbSet.Where(predicate);
        }

        public virtual void Add(TEntity entity) {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public virtual void AddRange(IEnumerable<TEntity> entities) {
            _dbSet.AddRange(entities);
            _context.SaveChanges();
        }

        public virtual void Update(TEntity entity) {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public virtual void Remove(TEntity entity) {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }

        public virtual void RemoveRange(IEnumerable<TEntity> entities) {
            _dbSet.RemoveRange(entities);
            _context.SaveChanges();
        }

        public virtual bool Exists(Func<TEntity, bool> predicate) {
            return _dbSet.Any(predicate);
        }
    }
}
