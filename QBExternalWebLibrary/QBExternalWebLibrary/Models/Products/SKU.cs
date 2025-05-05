using System.ComponentModel;

namespace QBExternalWebLibrary.Models.Products
{
    public class SKU
    {
        public int Id { get; set; }
        [DisplayName("Name")]
        public string Name { get; set; }
        public int? LengthId { get; set; }
        public Length? Length { get; set; }
        [DisplayName("Diameter ID")]
        public int DiameterId { get; set; }
        public Diameter Diameter { get; set; }
        [DisplayName("Product ID")]
        public int ProductIDId { get; set; }
        [DisplayName("Product ID")]
        public ProductID ProductId { get; set; }
    }

    public class SKUEditViewModel
    {
        public int Id { get; set; }
        [DisplayName("Length")] 
        public int? LengthId { get; set; }
        [DisplayName("Length")]
        public string LengthName { get; set; }
        [DisplayName("Name")]
        public string Name { get; set; }
        [DisplayName("Diameter")]
        public int DiameterId { get; set; }
        [DisplayName("Diameter")]
        public string DiameterName { get; set; }
        [DisplayName("Product ID")]
        public int ProductIDId { get; set; }
        [DisplayName("Product ID")]
        public string ProductIDName { get; set; }
    }
}
