using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Mapping;

public class QuickOrderTagMapper : IModelMapper<QuickOrderTag, QuickOrderTag>
{
    private readonly IRepository<QuickOrderTag> _repository;

    public QuickOrderTagMapper(IRepository<QuickOrderTag> repository)
    {
        _repository = repository;
    }

    public QuickOrderTag MapToModel(QuickOrderTag view)
    {
        var tag = _repository.GetById(view.Id);
        if (tag == null)
        {
            tag = new QuickOrderTag
            {
                Id = view.Id,
                QuickOrderId = view.QuickOrderId,
                Tag = view.Tag
            };
        }
        else
        {
            tag.Tag = view.Tag;
        }
        return tag;
    }

    public QuickOrderTag MapToEdit(QuickOrderTag model)
    {
        return model;
    }

    public List<QuickOrderTag> MapToEdit(IEnumerable<QuickOrderTag> models)
    {
        return models.ToList();
    }
}
