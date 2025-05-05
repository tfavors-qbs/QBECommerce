using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;

namespace QBExternalWebLibrary.Data.Repositories {
    public class ProductIDEFRepository : EFRepository<ProductID>, IRepository<ProductID> {
        public ProductIDEFRepository(DataContext context) : base(context) { }

        public override IEnumerable<ProductID> Find(Expression<Func<ProductID, bool>> predicate) {
            return _dbSet.Where(predicate)
                .Include(p => p.Coating)
                .Include(p => p.Group)
                    .ThenInclude(g => g.Class)
                .Include(p => p.Material)
                .Include(p => p.Shape)
                .Include(p => p.Spec)
                .Include(p => p.Thread);
        }

        public override IEnumerable<ProductID> GetAll() {
            return _dbSet
                .Include(p => p.Coating)
                .Include(p => p.Group)
                    .ThenInclude(g => g.Class)
                .Include(p => p.Material)
                .Include(p => p.Shape)
                .Include(p => p.Spec)
                .Include(p => p.Thread);

        }

        public override ProductID GetById(int? id) {
            return _dbSet.
                Include(p => p.Coating)
                .Include(p => p.Group)
                    .ThenInclude(g => g.Class)
                .Include(p => p.Material)
                .Include(p => p.Shape)
                .Include(p => p.Spec)
                .Include(p => p.Thread)
                .FirstOrDefault(m => m.Id == id);
        }
    }
}
