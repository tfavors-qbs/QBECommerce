using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Products;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Mapping {
    public class ContractItemMapper : IModelMapper<ContractItem, ContractItemEditViewModel> {
        IRepository<ContractItem> _contractItemRepository;
        IRepository<Client> _clientRepository;
        IRepository<SKU> _skuRepository;
        IRepository<Diameter> _diameterRepository;
        IRepository<Length> _lengthRepository;

        public ContractItemMapper(IRepository<ContractItem> contractItemRepository, IRepository<Client> clientRepository, IRepository<SKU> skuRepository,
            IRepository<Diameter> diameterRepository, IRepository<Length> lengthRepository) {
            _contractItemRepository = contractItemRepository;
            _clientRepository = clientRepository;
            _skuRepository = skuRepository;
            _diameterRepository = diameterRepository;
            _lengthRepository = lengthRepository;
        }

        public ContractItemEditViewModel MapToEdit(ContractItem model) {
            int classId, groupId, shapeId, materialId, coatingId, threadId, specId = 0;
            string className, groupName, shapeName, materialName, coatingName, threadName, specName;
            if (!model.NonStock) {
                classId = model.SKU.ProductId.Group.Class.Id;
                className = model.SKU.ProductId.Group.Class.DisplayName;
                groupId = model.SKU.ProductId.Group.Id;
                groupName = model.SKU.ProductId.Group.DisplayName;
                shapeId = model.SKU.ProductId.Shape.Id;
                shapeName = model.SKU.ProductId.Shape.DisplayName;
                materialId = model.SKU.ProductId.Material.Id;
                materialName = model.SKU.ProductId.Material.DisplayName;
                coatingId = model.SKU.ProductId.Coating.Id;
                coatingName = model.SKU.ProductId.Coating.DisplayName;
                threadId = model.SKU.ProductId.Thread.Id;
                threadName = model.SKU.ProductId.Thread.DisplayName;
                specId = model.SKU.ProductId.Spec.Id;
                specName = model.SKU.ProductId.Spec.DisplayName;

                return new ContractItemEditViewModel {
                    Id = model.Id,
                    CustomerStkNo = model.CustomerStkNo,
                    Description = model.Description,
                    Price = model.Price,
                    ClientId = model.ClientId,
                    SKUId = model.SKUId,
                    SKUName = model.SKU.Name,
                    DiameterId = model.DiameterId,
                    DiameterName = model.Diameter.DisplayName,
                    LengthId = model.LengthId,
                    LengthName = model.Length?.DisplayName,
                    NonStock = model.NonStock,
                    ClassId = classId,
                    ClassName = className,
                    GroupId = groupId,
                    GroupName = groupName,
                    ShapeId = shapeId,
                    ShapeName = shapeName,
                    MaterialId = materialId,
                    MaterialName = materialName,
                    CoatingId = coatingId,
                    CoatingName = coatingName,
                    ThreadId = threadId,
                    ThreadName = threadName,
                    SpecId = specId,
                    SpecName = specName,
                };
            } else
            return new ContractItemEditViewModel {
                Id = model.Id,
                CustomerStkNo = model.CustomerStkNo,
                Description = model.Description,
                Price = model.Price,
                ClientId = model.ClientId,
                SKUId = model.SKUId,
                SKUName = model.SKU.Name,
                DiameterId = model.DiameterId,
                DiameterName = model.Diameter.DisplayName,
                LengthId = model.LengthId,
                LengthName = model.Length.DisplayName,
                NonStock = model.NonStock,  
            };
        }

        public List<ContractItemEditViewModel> MapToEdit(IEnumerable<ContractItem> models) {
            List<ContractItemEditViewModel> evmItems = new List<ContractItemEditViewModel>();
            foreach (ContractItem item in models) {
                var viewModel = MapToEdit(item);
                evmItems.Add(viewModel);
            }
            return evmItems;
        }

        public ContractItem MapToModel(ContractItemEditViewModel view) {
            var contractItem = _contractItemRepository.GetById(view.Id);
            if (contractItem == null) {
                contractItem = new ContractItem {
                    Id = view.Id,
                    CustomerStkNo = view.CustomerStkNo,
                    Description = view.Description,
                    Price = view.Price,
                    ClientId = view.ClientId,
                    SKUId = view.SKUId,
                    DiameterId = view.DiameterId,
                    LengthId = view.LengthId,
                    NonStock = view.NonStock,
                    SKU = _skuRepository.GetById(view.SKUId),
                    Diameter = _diameterRepository.GetById(view.DiameterId),
                    Length = _lengthRepository.GetById(view.LengthId)
                };
            } else {
                contractItem.Id = view.Id;
                contractItem.CustomerStkNo = view.CustomerStkNo;
                contractItem.Description = view.Description;
                contractItem.Price = view.Price;
                contractItem.ClientId = view.ClientId;
                contractItem.SKUId = view.SKUId;
                contractItem.DiameterId = view.DiameterId;
                contractItem.LengthId = view.LengthId;
                contractItem.NonStock = view.NonStock;
                contractItem.SKU = _skuRepository.GetById(view.SKUId);
                contractItem.Diameter = _diameterRepository.GetById(view.DiameterId);
                contractItem.Length = _lengthRepository.GetById(view.LengthId);
            }
            if (view.NonStock) {
                contractItem.SKUId = null;
                contractItem.SKU = null;
            } else {
                contractItem.LengthId = contractItem.SKU.LengthId;
                contractItem.DiameterId = contractItem.SKU.DiameterId;
                contractItem.Length = _lengthRepository.GetById(view.LengthId);
                contractItem.Diameter = _diameterRepository.GetById(view.DiameterId);
            }
            return contractItem;
        }

    }
}
