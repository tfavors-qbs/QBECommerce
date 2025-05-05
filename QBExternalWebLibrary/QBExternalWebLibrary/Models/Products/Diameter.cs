using System.ComponentModel;

namespace QBExternalWebLibrary.Models.Products
{
    public class Diameter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [DisplayName("Diameter")]
        public string DisplayName { get; set; }
        public double Value { get; set; }
    }
}
