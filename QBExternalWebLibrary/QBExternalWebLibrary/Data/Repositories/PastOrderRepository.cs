using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Catalog;
using System.Linq.Expressions;

namespace QBExternalWebLibrary.Data.Repositories;

public class PastOrderRepository : EFRepository<PastOrder>
{
    public PastOrderRepository(DataContext context) : base(context)
    {
    }

    public override IEnumerable<PastOrder> FindFullyIncluded(Expression<Func<PastOrder, bool>> predicate)
    {
        return _dbSet
            .Include(p => p.Items)
                .ThenInclude(i => i.ContractItem)
                    .ThenInclude(c => c.SKU)
            .Include(p => p.Tags)
            .Include(p => p.User)
            .Include(p => p.Client)
            .Where(predicate)
            .ToList();
    }
}
