using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Data.Repositories;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using Microsoft.EntityFrameworkCore;
using Ariba;
using System.Threading;

namespace QBExternalWebLibrary.Models.Mapping {
    public class ProductIDMapper : IModelMapper<ProductID, ProductIDEditViewModel> {
        IRepository<ProductID> _productIDRepository;
        IRepository<Group> _groupRepository;
        IRepository<Shape> _shapeRepository;
        IRepository<Material> _materialRepository;
        IRepository<Coating> _coatingRepository;
        IRepository<Thread> _threadRepository;
        IRepository<Spec> _specRepository;

        public ProductIDMapper(IRepository<ProductID> productIDRepository, IRepository<Group> groupRepository, IRepository<Shape> shapeRepository,
            IRepository<Material> materialRepository, IRepository<Coating> coatingRepository, IRepository<Thread> threadRepository, IRepository<Spec> specRepository) {
            _productIDRepository = productIDRepository;
            _groupRepository = groupRepository;
            _shapeRepository = shapeRepository;
            _materialRepository = materialRepository;
            _coatingRepository = coatingRepository;
            _threadRepository = threadRepository;
            _specRepository = specRepository;
        }

        public ProductIDEditViewModel MapToEdit(ProductID model) {
            return new ProductIDEditViewModel {
                Id = model.Id,
                GroupId = model.GroupId,
                ShapeId = model.ShapeId,
                MaterialId = model.MaterialId,
                CoatingId = model.CoatingId,
                ThreadId = model.ThreadId,
                SpecId = model.SpecId,
                LegacyId = model.LegacyId,
                LegacyName = model.LegacyName,
            };
        }

        public List<ProductIDEditViewModel> MapToEdit(IEnumerable<ProductID> models) {
            throw new NotImplementedException();
        }

        public ProductID MapToModel(ProductIDEditViewModel view) {
            var productID = _productIDRepository.GetById(view.Id);
            if (productID == null) {
                productID = new ProductID {
                    Id = view.Id,
                    GroupId = view.GroupId,
                    ShapeId = view.ShapeId,
                    MaterialId = view.MaterialId,
                    CoatingId = view.CoatingId,
                    ThreadId = view.ThreadId,
                    SpecId = view.SpecId,
                    LegacyId = view.LegacyId,
                    LegacyName = view.LegacyName,
                    Description = view.Description,
                    Group = _groupRepository.GetById(view.GroupId),
                    Shape = _shapeRepository.GetById(view.ShapeId),
                    Material = _materialRepository.GetById(view.MaterialId),
                    Coating = _coatingRepository.GetById(view.CoatingId),
                    Thread = _threadRepository.GetById(view.ThreadId),
                    Spec = _specRepository.GetById(view.SpecId)
                };
            } else {
                productID.Id = view.Id;
                productID.GroupId = view.GroupId;
                productID.ShapeId = view.ShapeId;
                productID.MaterialId = view.MaterialId;
                productID.CoatingId = view.CoatingId;
                productID.ThreadId = view.ThreadId;
                productID.SpecId = view.SpecId;
                productID.LegacyId = view.LegacyId;
                productID.LegacyName = view.LegacyName;
                productID.Description = view.Description;
                productID.Group = _groupRepository.GetById(view.GroupId);
                productID.Shape = _shapeRepository.GetById(view.ShapeId);
                productID.Material = _materialRepository.GetById(view.MaterialId);
                productID.Coating = _coatingRepository.GetById(view.CoatingId);
                productID.Thread = _threadRepository.GetById(view.ThreadId);
                productID.Spec = _specRepository.GetById(view.SpecId);
            }
            return productID;
        }
    }
}
