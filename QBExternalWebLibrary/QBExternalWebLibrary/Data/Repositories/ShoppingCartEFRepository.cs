using QBExternalWebLibrary.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Data.Repositories {
    public class ShoppingCartEFRepository : EFRepository<ShoppingCart>, IRepository<ShoppingCart> {
        public ShoppingCartEFRepository(DataContext context) : base(context) {
        }

        public override IEnumerable<ShoppingCart> Find(Expression<Func<ShoppingCart, bool>> predicate) {
            return _dbSet.Where(predicate);
        }

        public override IEnumerable<ShoppingCart> GetAll() {
            return _dbSet;
        }

        public override ShoppingCart GetById(int? id) {
            return _dbSet.FirstOrDefault(s => s.Id == id);
        }
    }
}
