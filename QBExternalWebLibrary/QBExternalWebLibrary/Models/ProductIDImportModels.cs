using System.ComponentModel.DataAnnotations;

namespace QBExternalWebLibrary.Models
{
    /// <summary>
    /// Request model for bulk ProductID import
    /// </summary>
    public class ProductIDImportRequest
    {
        [Required]
        public List<ProductIDImportDto> ProductIDs { get; set; } = new();
    }

    /// <summary>
    /// DTO for creating a single ProductID via import
    /// </summary>
    public class ProductIDImportDto
    {
        [Required]
        public int LegacyId { get; set; }
        
        [Required]
        public string LegacyName { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public DimensionImportDto Group { get; set; }
        
        [Required]
        public SpecificationTypeImportDto Shape { get; set; }
        
        [Required]
        public SpecificationTypeImportDto Material { get; set; }
        
        [Required]
        public SpecificationTypeImportDto Coating { get; set; }
        
        [Required]
        public SpecificationTypeImportDto Thread { get; set; }
        
        [Required]
        public SpecificationTypeImportDto Spec { get; set; }
    }

    /// <summary>
    /// DTO for dimension data that may need to be created (Group)
    /// </summary>
    public class DimensionImportDto
    {
        [Required]
        public string LegacyId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string DisplayName { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public ClassImportDto Class { get; set; }
    }

    /// <summary>
    /// DTO for class data that may need to be created
    /// </summary>
    public class ClassImportDto
    {
        [Required]
        public string LegacyId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string DisplayName { get; set; }
        
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for specification types (Shape, Material, Coating, Thread, Spec)
    /// </summary>
    public class SpecificationTypeImportDto
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string DisplayName { get; set; }
        
        public string? Description { get; set; }
    }

    /// <summary>
    /// Response model for bulk ProductID import
    /// </summary>
    public class ProductIDImportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalRequested { get; set; }
        public int SuccessfullyCreated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<ProductIDImportResult> Results { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Individual result for each ProductID in the import
    /// </summary>
    public class ProductIDImportResult
    {
        public string LegacyName { get; set; }
        public int LegacyId { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; } // "Created", "Skipped", "Failed"
        public string? Reason { get; set; }
        public int? CreatedProductIDId { get; set; }
    }
}
