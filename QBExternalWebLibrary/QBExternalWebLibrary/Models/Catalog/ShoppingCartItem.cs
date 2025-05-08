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
        public int Quantity { get; set; }
    }
}
