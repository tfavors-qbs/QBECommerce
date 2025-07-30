using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Ariba;

namespace QBExternalWebLibrary.Models.Catalog
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        [ForeignKey("ApplicationUser")]
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public List<ShoppingCartItem>? ShoppingCartItems { get; set; }
    }

    public class ShoppingCartEVM {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = "";
    }

    public class PunchOutOrderMesssageGenerator()
    {
        public void Generate()
        {
            
        }
    }
}
