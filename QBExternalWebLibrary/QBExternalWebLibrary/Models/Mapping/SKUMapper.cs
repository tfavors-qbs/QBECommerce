using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace QBExternalWebLibrary.Models.Mapping {
    public class SKUMapper : IModelMapper<SKU, SKUEditViewModel> {
        private readonly IRepository<SKU> _skuRepository;
        private readonly IRepository<Length> _lengthRepository;
        private readonly IRepository<Diameter> _diameterRepository;
        private readonly IRepository<ProductID> _productIDRepository;

        public SKUMapper(IRepository<SKU> skuRepository, IRepository<Length> lengthRepository, IRepository<Diameter> diameterRepository, IRepository<ProductID> productIDRepository) {
            _skuRepository = skuRepository;
            _lengthRepository = lengthRepository;
            _diameterRepository = diameterRepository;
            _productIDRepository = productIDRepository;
        }

        public SKUEditViewModel MapToEdit(SKU model) {
            return new SKUEditViewModel {
                Id = model.Id,
                Name = model.Name,
                LengthId = model.LengthId,
                DiameterId = model.DiameterId,
                ProductIDId = model.ProductIDId,
                LengthName = model.Length?.DisplayName,
                DiameterName = model.Diameter?.DisplayName,
                ProductIDName = model.ProductId?.LegacyName
            };
        }

        public List<SKUEditViewModel> MapToEdit(IEnumerable<SKU> models) {
            List<SKUEditViewModel> evmItems = new List<SKUEditViewModel>();
            foreach (SKU item in models) {
                var viewModel = MapToEdit(item);
                evmItems.Add(viewModel);
            }
            return evmItems;
        }

        public SKU MapToModel(SKUEditViewModel view) {
            var sku = _skuRepository.GetById(view.Id);
            if (sku == null) {
                sku = new SKU {
                    Id = view.Id,
                    Name = view.Name,
                    LengthId = view.LengthId,
                    DiameterId = view.DiameterId,
                    ProductIDId = view.ProductIDId,
                    Length = _lengthRepository.GetById(view.LengthId),
                    Diameter = _diameterRepository.GetById(view.DiameterId),
                    ProductId = _productIDRepository.GetById(view.ProductIDId),
                };
            } else {
                sku.Id = view.Id;
                sku.LengthId = view.LengthId;
                sku.Name = view.Name;
                sku.DiameterId = view.DiameterId;
                sku.ProductIDId = view.ProductIDId;
                sku.Length = _lengthRepository.GetById(view.LengthId);
                sku.Diameter = _diameterRepository.GetById(view.DiameterId);
                sku.ProductId = _productIDRepository.GetById(view.ProductIDId);
            }
            return sku;
        }
    }
}
