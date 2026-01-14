using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Mapping;

public class QuickOrderItemMapper : IModelMapper<QuickOrderItem, QuickOrderItemEVM>
{
    private readonly IRepository<QuickOrderItem> _repository;
    private readonly IModelMapper<ContractItem, ContractItemEditViewModel> _contractItemMapper;

    public QuickOrderItemMapper(
        IRepository<QuickOrderItem> repository,
        IModelMapper<ContractItem, ContractItemEditViewModel> contractItemMapper)
    {
        _repository = repository;
        _contractItemMapper = contractItemMapper;
    }

    public QuickOrderItem MapToModel(QuickOrderItemEVM view)
    {
        var item = _repository.GetById(view.Id);
        if (item == null)
        {
            item = new QuickOrderItem
            {
                Id = view.Id,
                QuickOrderId = view.QuickOrderId,
                ContractItemId = view.ContractItemId,
                Quantity = view.Quantity
            };
        }
        else
        {
            item.QuickOrderId = view.QuickOrderId;
            item.ContractItemId = view.ContractItemId;
            item.Quantity = view.Quantity;
        }
        return item;
    }

    public QuickOrderItemEVM MapToEdit(QuickOrderItem model)
    {
        return new QuickOrderItemEVM
        {
            Id = model.Id,
            QuickOrderId = model.QuickOrderId,
            ContractItemId = model.ContractItemId,
            ContractItem = model.ContractItem != null ? _contractItemMapper.MapToEdit(model.ContractItem) : null,
            Quantity = model.Quantity,
            IsAvailable = model.ContractItem != null
        };
    }

    public List<QuickOrderItemEVM> MapToEdit(IEnumerable<QuickOrderItem> models)
    {
        return models.Select(MapToEdit).ToList();
    }
}
