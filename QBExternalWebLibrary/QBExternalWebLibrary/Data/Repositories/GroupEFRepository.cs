using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;

namespace QBExternalWebLibrary.Data.Repositories {
    public class GroupEFRepository : EFRepository<Group>, IRepository<Group> {
        public GroupEFRepository(DataContext context) : base(context) {
        }

        public override IEnumerable<Group> Find(Expression<Func<Group, bool>> predicate) {
            return _dbSet.Where(predicate)
                .Include(g => g.Class);
        }

        public override IEnumerable<Group> GetAll() {
            return _dbSet
                .Include(g => g.Class);
        }

        public override Group GetById(int? id) {
            return _dbSet
                .Include(g => g.Class)
                .FirstOrDefault(m => m.Id == id);
        }
    }
}
