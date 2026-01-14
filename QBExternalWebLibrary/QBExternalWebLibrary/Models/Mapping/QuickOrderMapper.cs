using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Mapping;

public class QuickOrderMapper : IModelMapper<QuickOrder, QuickOrderEVM>
{
    private readonly IRepository<QuickOrder> _repository;

    public QuickOrderMapper(IRepository<QuickOrder> repository)
    {
        _repository = repository;
    }

    public QuickOrder MapToModel(QuickOrderEVM view)
    {
        var quickOrder = _repository.GetById(view.Id);
        if (quickOrder == null)
        {
            quickOrder = new QuickOrder
            {
                Id = view.Id,
                Name = view.Name,
                OwnerId = view.OwnerId,
                IsSharedClientWide = view.IsSharedClientWide,
                CreatedAt = view.CreatedAt,
                LastUsedAt = view.LastUsedAt,
                TimesUsed = view.TimesUsed,
                IsDeleted = view.IsDeleted,
                DeletedAt = view.DeletedAt
            };
        }
        else
        {
            quickOrder.Name = view.Name;
            quickOrder.IsSharedClientWide = view.IsSharedClientWide;
            quickOrder.LastUsedAt = view.LastUsedAt;
            quickOrder.TimesUsed = view.TimesUsed;
            quickOrder.IsDeleted = view.IsDeleted;
            quickOrder.DeletedAt = view.DeletedAt;
        }
        return quickOrder;
    }

    public QuickOrderEVM MapToEdit(QuickOrder model)
    {
        return new QuickOrderEVM
        {
            Id = model.Id,
            Name = model.Name,
            OwnerId = model.OwnerId,
            OwnerName = model.Owner != null ? $"{model.Owner.GivenName} {model.Owner.FamilyName}".Trim() : null,
            OwnerEmail = model.Owner?.Email,
            IsSharedClientWide = model.IsSharedClientWide,
            CreatedAt = model.CreatedAt,
            LastUsedAt = model.LastUsedAt,
            TimesUsed = model.TimesUsed,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt,
            ItemCount = model.Items?.Count ?? 0,
            TotalValue = model.Items?.Sum(i => i.Quantity * (i.ContractItem?.Price ?? 0)) ?? 0,
            Tags = model.Tags?.Select(t => t.Tag).ToList() ?? new()
        };
    }

    public List<QuickOrderEVM> MapToEdit(IEnumerable<QuickOrder> models)
    {
        return models.Select(MapToEdit).ToList();
    }
}
