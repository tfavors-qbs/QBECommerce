using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Data.Repositories {
    public class ClientEFRepository : EFRepository<Client>, IRepository<Client> {
        public ClientEFRepository(DataContext context) : base(context) {
        }

        public override IEnumerable<Client> Find(Expression<Func<Client, bool>> predicate) {
            return _dbSet.Where(predicate);
        }

        public override IEnumerable<Client> GetAll() {
            return _dbSet;

        }

        public override Client GetById(int? id) {
            return _dbSet
                .FirstOrDefault(m => m.Id == id);
        }
    }
}
