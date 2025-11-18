using System.ComponentModel.DataAnnotations;

namespace QBExternalWebLibrary.Models
{
    public class ClientImportRequest
    {
        [Required]
        public ClientDto Client { get; set; }
        
        [Required]
        public List<ContractItemDto> ContractItems { get; set; } = new();
    }

    public class ClientDto
    {
        [Required]
        public string LegacyId { get; set; }
        
        [Required]
        public string Name { get; set; }
    }

    public class ContractItemDto
    {
        [Required]
        public string CustomerStkNo { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        
        public SKUDto? SKU { get; set; }
        public DimensionDto? Diameter { get; set; }
        public DimensionDto? Length { get; set; }
        public bool NonStock { get; set; }
    }

    public class SKUDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public DimensionDto Diameter { get; set; }
        
        public DimensionDto? Length { get; set; }
        
        [Required]
        public ProductIDDto ProductID { get; set; }
    }

    public class ProductIDDto
    {
        [Required]
        public int LegacyId { get; set; }
        
        [Required]
        public string LegacyName { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public GroupDto Group { get; set; }
        
        [Required]
        public DimensionDto Shape { get; set; }
        
        [Required]
        public DimensionDto Material { get; set; }
        
        [Required]
        public DimensionDto Coating { get; set; }
        
        [Required]
        public DimensionDto Thread { get; set; }
        
        [Required]
        public DimensionDto Spec { get; set; }
    }

    public class GroupDto
    {
        [Required]
        public string LegacyId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string DisplayName { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public ClassDto Class { get; set; }
    }

    public class ClassDto
    {
        [Required]
        public string LegacyId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string DisplayName { get; set; }
        
        public string? Description { get; set; }
    }

    public class DimensionDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string DisplayName { get; set; }
        
        public double? Value { get; set; }  // For Length and Diameter
        public string? Description { get; set; }
    }

    public class ClientImportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ClientName { get; set; }
        public int ClientId { get; set; }
        public bool IsNewClient { get; set; }
        public int ImportedItemsCount { get; set; }
        public List<ImportError> SkippedItems { get; set; } = new();
        public List<ImportError> FailedItems { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }

    public class ImportError
    {
        public string CustomerStkNo { get; set; }
        public string Reason { get; set; }
    }
}
