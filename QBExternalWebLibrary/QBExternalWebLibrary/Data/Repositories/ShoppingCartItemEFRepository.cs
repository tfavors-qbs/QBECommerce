using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Data.Repositories {
    public class ShoppingCartItemEFRepository : EFRepository<ShoppingCartItem>, IRepository<ShoppingCartItem> {
        public ShoppingCartItemEFRepository(DataContext context) : base(context) {
        }

        public override IEnumerable<ShoppingCartItem> Find(Expression<Func<ShoppingCartItem, bool>> predicate) {
            return _dbSet.Where(predicate);
        }

        public override IEnumerable<ShoppingCartItem> GetAll() {
            return _dbSet;
        }

        public override ShoppingCartItem GetById(int? id) {
            return _dbSet.FirstOrDefault(s => s.Id == id);
        }

		public override IEnumerable<ShoppingCartItem> FindFullyIncluded(Expression<Func<ShoppingCartItem, bool>> predicate)
		{
            return _dbSet.Where(predicate).Include(a => a.ContractItem).ThenInclude(a => a.SKU);
		}
	}
}
