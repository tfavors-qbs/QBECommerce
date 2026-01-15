using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Mapping;

public class PastOrderMapper : IModelMapper<PastOrder, PastOrderEVM>
{
    private readonly IRepository<PastOrder> _repository;

    public PastOrderMapper(IRepository<PastOrder> repository)
    {
        _repository = repository;
    }

    public PastOrder MapToModel(PastOrderEVM view)
    {
        var pastOrder = _repository.GetById(view.Id);
        if (pastOrder == null)
        {
            pastOrder = new PastOrder
            {
                Id = view.Id,
                UserId = view.UserId,
                ClientId = view.ClientId,
                PONumber = view.PONumber,
                OrderedAt = view.OrderedAt,
                TotalAmount = view.TotalAmount,
                ItemCount = view.ItemCount
            };
        }
        else
        {
            pastOrder.PONumber = view.PONumber;
            // Note: Most fields should not be updated after creation
        }
        return pastOrder;
    }

    public PastOrderEVM MapToEdit(PastOrder model)
    {
        return new PastOrderEVM
        {
            Id = model.Id,
            PONumber = model.PONumber,
            OrderedAt = model.OrderedAt,
            TotalAmount = model.TotalAmount,
            ItemCount = model.ItemCount,
            UserId = model.UserId,
            UserName = model.User != null ? $"{model.User.GivenName} {model.User.FamilyName}".Trim() : null,
            UserEmail = model.User?.Email,
            ClientId = model.ClientId,
            Tags = model.Tags?.Select(t => t.Tag).ToList() ?? new()
        };
    }

    public List<PastOrderEVM> MapToEdit(IEnumerable<PastOrder> models)
    {
        return models.Select(MapToEdit).ToList();
    }
}

public class PastOrderItemMapper : IModelMapper<PastOrderItem, PastOrderItemEVM>
{
    private readonly IRepository<PastOrderItem> _repository;

    public PastOrderItemMapper(IRepository<PastOrderItem> repository)
    {
        _repository = repository;
    }

    public PastOrderItem MapToModel(PastOrderItemEVM view)
    {
        var item = _repository.GetById(view.Id);
        if (item == null)
        {
            item = new PastOrderItem
            {
                Id = view.Id,
                PastOrderId = view.PastOrderId,
                ContractItemId = view.ContractItemId,
                Quantity = view.Quantity,
                UnitPrice = view.UnitPrice
            };
        }
        return item;
    }

    public PastOrderItemEVM MapToEdit(PastOrderItem model)
    {
        return new PastOrderItemEVM
        {
            Id = model.Id,
            PastOrderId = model.PastOrderId,
            ContractItemId = model.ContractItemId,
            ProductName = model.ContractItem?.SKU?.Name ?? "Unknown",
            Description = model.ContractItem?.Description ?? "",
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice,
            IsAvailable = model.ContractItem != null
        };
    }

    public List<PastOrderItemEVM> MapToEdit(IEnumerable<PastOrderItem> models)
    {
        return models.Select(MapToEdit).ToList();
    }
}

public class PastOrderTagMapper : IModelMapper<PastOrderTag, PastOrderTag>
{
    public PastOrderTag MapToModel(PastOrderTag view) => view;
    public PastOrderTag MapToEdit(PastOrderTag model) => model;
    public List<PastOrderTag> MapToEdit(IEnumerable<PastOrderTag> models) => models.ToList();
}
