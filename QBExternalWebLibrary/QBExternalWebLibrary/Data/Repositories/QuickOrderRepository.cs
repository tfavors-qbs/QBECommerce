using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Catalog;
using System.Linq.Expressions;

namespace QBExternalWebLibrary.Data.Repositories;

public class QuickOrderRepository : EFRepository<QuickOrder>
{
    public QuickOrderRepository(DataContext context) : base(context)
    {
    }

    public override IEnumerable<QuickOrder> FindFullyIncluded(Expression<Func<QuickOrder, bool>> predicate)
    {
        return _dbSet
            .Include(q => q.Items)
                .ThenInclude(i => i.ContractItem)
            .Include(q => q.Tags)
            .Include(q => q.Owner)
            .Where(predicate)
            .ToList();
    }
}
