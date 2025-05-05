using Ariba;
using QBExternalWebLibrary.Models.Products;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models {
    public class ContractItem {
        public int Id { get; set; }

        [DisplayName("Customer Stock Number")]
        public string CustomerStkNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,4)")]
        public decimal Price { get; set; } 
        public int ClientId { get; set; }
        public Client Client { get; set; }
        public int? SKUId { get; set; }
        public SKU? SKU { get; set; }
        public int? DiameterId { get; set; }
        public Diameter? Diameter { get; set; }
        public int? LengthId { get; set; }
        public Length? Length { get; set; }
        public bool NonStock { get; set; }       
    }

    public class ContractItemEditViewModel {
        public int Id { get; set; }
        [DisplayName("Customer Stock Number")]
        public string CustomerStkNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        [DisplayName("Client")]
        public int ClientId { get; set; }
        [DisplayName("Client")]
        public string ClientName { get; set; }
        [DisplayName("SKU")]
        public int? SKUId { get; set; }
        [DisplayName("SKU")]
        public string? SKUName { get; set; }
        [DisplayName("Diameter")]
        public int? DiameterId { get; set; }
        [DisplayName("Diameter")]
        public string? DiameterName { get; set; }
        [DisplayName("Length")]
        public int? LengthId { get; set; }
        [DisplayName("Length")]
        public string? LengthName { get; set; }
        [DisplayName("Class")]
        public int? ClassId { get; set; }
        [DisplayName("Class")]
        public string? ClassName { get; set; }
        [DisplayName("Group")]
        public int? GroupId { get; set; }
        [DisplayName("Group")]
        public string? GroupName { get; set; }
        [DisplayName("Shape")]
        public int? ShapeId { get; set; }
        [DisplayName("Shape")]
        public string? ShapeName { get; set; }
        [DisplayName("Material")]
        public int? MaterialId { get; set; }
        [DisplayName("Material")]
        public string? MaterialName { get; set; }
        [DisplayName("Coating")]
        public int? CoatingId { get; set; }
        [DisplayName("Coating")]
        public string? CoatingName { get; set; }
        [DisplayName("Thread")]
        public int? ThreadId { get; set; }
        [DisplayName("Thread")]
        public string? ThreadName { get; set; }
        [DisplayName("Spec")]
        public int? SpecId { get; set; }
        [DisplayName("Spec")]
        public string? SpecName { get; set; }
        [DisplayName("NonStock")]
        public bool NonStock { get; set; }        
    }
}
