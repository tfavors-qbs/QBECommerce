using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Mapping {
    public class ShoppingCartItemMapper : IModelMapper<ShoppingCartItem, ShoppingCartItemEVM> {
        private readonly IRepository<ShoppingCartItem> _repository;

        public ShoppingCartItemMapper(IRepository<ShoppingCartItem> repository) {
            _repository = repository;
        }

        public ShoppingCartItem MapToModel(ShoppingCartItemEVM view) {
            var shoppingCartItem = _repository.GetById(view.Id);
            if (shoppingCartItem == null) {
                shoppingCartItem = new ShoppingCartItem {
                    Id = view.Id,
                    ShoppingCartId = view.ShoppingCartId,
                    Quantity = view.Quantity
                };
            } else {
                shoppingCartItem.ShoppingCartId = view.ShoppingCartId;
                shoppingCartItem.Quantity = view.Quantity;
            }
            return shoppingCartItem;
        }

        public ShoppingCartItemEVM MapToEdit(ShoppingCartItem model) {
            return new ShoppingCartItemEVM {
                Id = model.Id,
                ShoppingCartId= model.ShoppingCartId,
                Quantity = model.Quantity
            };
        }

        public List<ShoppingCartItemEVM> MapToEdit(List<ShoppingCartItemEVM> list) {
            throw new NotImplementedException();
        }

        public List<ShoppingCartItemEVM> MapToEdit(IEnumerable<ShoppingCartItem> models) {
            throw new NotImplementedException();
        }
    }
}
