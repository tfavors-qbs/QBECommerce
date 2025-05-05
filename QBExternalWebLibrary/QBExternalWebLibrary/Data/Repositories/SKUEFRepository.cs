using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBExternalWebLibrary.Models.Products;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Services;

namespace QBExternalWebLibrary.Data.Repositories {
    public class SKUEFRepository : EFRepository <SKU>, IRepository<SKU> {

        public SKUEFRepository(DataContext context) : base(context) { }

        public override IEnumerable<SKU> Find(Expression<Func<SKU, bool>> predicate) {
            return _dbSet.Where(predicate)
                .Include(s => s.Diameter)
                .Include(s => s.Length)
                .Include(s => s.ProductId)
                        .ThenInclude(p => p.Group)
                        .ThenInclude(g => g.Class).ToList();
        }

        public override IEnumerable<SKU> GetAll() {
            return _dbSet
                .Include(s => s.Diameter)
                .Include(s => s.Length)
                .Include(s => s.ProductId)
                        .ThenInclude(p => p.Group)
                        .ThenInclude(g => g.Class).ToList();
        }

        public override SKU GetById(int? id) {
            return _dbSet
                .Include(s => s.Diameter)
                .Include(s => s.Length)
                .Include(s => s.ProductId)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g.Class)
                    .FirstOrDefault(m => m.Id == id);
        }
    }
}
