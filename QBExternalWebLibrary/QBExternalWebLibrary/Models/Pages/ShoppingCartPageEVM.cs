using QBExternalWebLibrary.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Pages {
    public class ShoppingCartPageEVM {
        public ShoppingCartEVM ShoppingCartEVM { get; set; }
        public Dictionary<int, ShoppingCartItemEVM> ShoppingCartItemEVMs { get; set; }
    }
}
