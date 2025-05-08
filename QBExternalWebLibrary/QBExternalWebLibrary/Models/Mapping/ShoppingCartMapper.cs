using QBExternalWebLibrary.Data.Repositories;
using QBExternalWebLibrary.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Mapping {
    public class ShoppingCartMapper : IModelMapper<ShoppingCart, ShoppingCartEVM> {
        private readonly IRepository<ShoppingCart> _repository;

        public ShoppingCartMapper(IRepository<ShoppingCart> repository) {
            _repository = repository;
        }

        public ShoppingCart MapToModel(ShoppingCartEVM view) {
            var shoppingCart = _repository.GetById(view.Id);
            if (shoppingCart == null) {
                shoppingCart = new ShoppingCart {
                    Id = view.Id,
                    ApplicationUserId = view.ApplicationUserId
                };
            } else {
                shoppingCart.ApplicationUserId = view.ApplicationUserId;
            }
            return shoppingCart;
        }

        public ShoppingCartEVM MapToEdit(ShoppingCart model) {
            return new ShoppingCartEVM {
                Id = model.Id,
                ApplicationUserId = model.ApplicationUserId,
            };
        }

        public List<ShoppingCartEVM> MapToEdit(List<ShoppingCartEVM> list) {
            throw new NotImplementedException();
        }

        public List<ShoppingCartEVM> MapToEdit(IEnumerable<ShoppingCart> models) {
            throw new NotImplementedException();
        }
    }
}
