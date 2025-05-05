using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Data.Repositories {
    public class ContractItemEFRepository : EFRepository<ContractItem>, IRepository<ContractItem> {
        public ContractItemEFRepository(DataContext context) : base(context) { }

        public override IEnumerable<ContractItem> Find(Expression<Func<ContractItem, bool>> predicate) {
            return _dbSet.Where(predicate)
                .Include(c => c.Client)
                .Include(c => c.SKU)
                .Include(c => c.Length)
                .Include(c => c.Diameter);
        }

        public override IEnumerable<ContractItem> GetAll() {
            return _dbSet
                .Include(c => c.Client)
                .Include(c => c.SKU).ThenInclude(s => s.ProductId).ThenInclude(p => p.Group).ThenInclude(g => g.Class)
                .Include(c => c.SKU).ThenInclude(s => s.ProductId).ThenInclude(p => p.Shape)
                .Include(c => c.SKU).ThenInclude(s => s.ProductId).ThenInclude(s => s.Material)
                .Include(c => c.SKU).ThenInclude(s => s.ProductId).ThenInclude(s => s.Coating)
                .Include(c => c.SKU).ThenInclude(s => s.ProductId).ThenInclude(p => p.Thread)
                .Include(c => c.SKU).ThenInclude(s => s.ProductId).ThenInclude(p => p.Spec)
                .Include(c => c.Length)
                .Include(c => c.Diameter);
        }

        public override ContractItem GetById(int? id) {
            return _dbSet.Include(c => c.Client)
                .Include(c => c.SKU)
                .Include(c => c.Length)
                .Include(c => c.Diameter)
                .FirstOrDefault(m => m.Id == id);
        }
    }
}
