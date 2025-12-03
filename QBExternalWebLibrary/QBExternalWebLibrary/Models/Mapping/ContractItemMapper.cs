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
            // Debug logging
            Console.WriteLine($"[ContractItemMapper] Mapping Item {model.Id} ({model.CustomerStkNo}): LengthId={model.LengthId}, Length={(model.Length != null ? model.Length.DisplayName : "NULL")}, SKU.LengthId={model.SKU?.LengthId}, SKU.Length={model.SKU?.Length?.DisplayName}");

            int classId, groupId, shapeId, materialId, coatingId, threadId, specId = 0;
            string className, groupName, shapeName, materialName, coatingName, threadName, specName;
            
            // Use ContractItem's ProductID if set, otherwise fall back to SKU's ProductID
            var productId = model.ProductID ?? model.SKU?.ProductId;
            
            // For stock items, get Length/Diameter from SKU if not set on ContractItem
            var length = model.Length ?? model.SKU?.Length;
            var diameter = model.Diameter ?? model.SKU?.Diameter;
            var lengthId = model.LengthId ?? model.SKU?.LengthId;
            var diameterId = model.DiameterId ?? model.SKU?.DiameterId;
            
            if (productId != null) {
                classId = productId.Group?.Class?.Id ?? 0;
                className = productId.Group?.Class?.DisplayName;
                groupId = productId.Group?.Id ?? 0;
                groupName = productId.Group?.DisplayName;
                shapeId = productId.Shape?.Id ?? 0;
                shapeName = productId.Shape?.DisplayName;
                materialId = productId.Material?.Id ?? 0;
                materialName = productId.Material?.DisplayName;
                coatingId = productId.Coating?.Id ?? 0;
                coatingName = productId.Coating?.DisplayName;
                threadId = productId.Thread?.Id ?? 0;
                threadName = productId.Thread?.DisplayName;
                specId = productId.Spec?.Id ?? 0;
                specName = productId.Spec?.DisplayName;

                return new ContractItemEditViewModel {
                    Id = model.Id,
                    CustomerStkNo = model.CustomerStkNo,
                    Description = model.Description,
                    Price = model.Price,
                    ClientId = model.ClientId,
                    SKUId = model.SKUId,
                    SKUName = model.SKU?.Name,
                    ProductIDId = model.ProductIDId ?? model.SKU?.ProductIDId,
                    ProductIDName = productId.LegacyName,
                    DiameterId = diameterId,
                    DiameterName = diameter?.DisplayName,
                    LengthId = lengthId,
                    LengthName = length?.DisplayName,
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
                SKUName = model.SKU?.Name,
                ProductIDId = model.ProductIDId,
                ProductIDName = null,
                DiameterId = diameterId,
                DiameterName = diameter?.DisplayName,
                LengthId = lengthId,
                LengthName = length?.DisplayName,
                NonStock = model.NonStock,  
            };
        }

        public List<ContractItemEditViewModel> MapToEdit(IEnumerable<ContractItem> models) {
            List<ContractItemEditViewModel> evmItems = new List<ContractItemEditViewModel>();
            foreach (ContractItem item in models) {
                try {
                    var viewModel = MapToEdit(item);
                    evmItems.Add(viewModel);
                } catch (Exception ex) {
                    Console.WriteLine($"[ContractItemMapper] Error mapping contract item {item.Id}: {ex.Message}");
                    Console.WriteLine($"[ContractItemMapper] Item details - CustomerStkNo: {item.CustomerStkNo}, NonStock: {item.NonStock}, SKU: {item.SKU?.Name ?? "null"}, SKUId: {item.SKUId}");
                    throw new Exception($"Failed to map contract item {item.Id} ({item.CustomerStkNo}): {ex.Message}", ex);
                }
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
                    ProductIDId = view.ProductIDId,
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
                contractItem.ProductIDId = view.ProductIDId;
                contractItem.DiameterId = view.DiameterId;
                contractItem.LengthId = view.LengthId;
                contractItem.NonStock = view.NonStock;
                contractItem.SKU = _skuRepository.GetById(view.SKUId);
                contractItem.Diameter = _diameterRepository.GetById(view.DiameterId);
                contractItem.Length = _lengthRepository.GetById(view.LengthId);
            }
            if (view.NonStock) {
                // Don't set SKUId to null - keep it as #XN for non-stock items
                // contractItem.SKUId = null;
                // contractItem.SKU = null;
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
