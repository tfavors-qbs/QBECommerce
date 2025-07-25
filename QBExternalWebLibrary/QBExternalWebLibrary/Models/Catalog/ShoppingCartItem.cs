using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Catalog {
    public class ShoppingCartItem {
        public int Id { get; set; }
        public int ShoppingCartId { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
        public int ContractItemId { get; set; }
        public ContractItem ContractItem { get; set; }
        public int Quantity { get; set; }
    }

    public class ShoppingCartItemEVM {
        public int Id { get; set; }
        public int ShoppingCartId { get; set; }
        public int ContractItemId { get; set; }
        public ContractItemEditViewModel ContractItemEditViewModel { get; set; }
        public int Quantity { get; set; }

        public ShoppingCartItemEVM Copy() 
        {
            return new ShoppingCartItemEVM()
            {
                Id = Id,
                ShoppingCartId = ShoppingCartId,
                ContractItemId = ContractItemId,
                ContractItemEditViewModel = ContractItemEditViewModel, //TODO: could maybe not be a good idea
                Quantity = Quantity
            };
        }
    }
}
