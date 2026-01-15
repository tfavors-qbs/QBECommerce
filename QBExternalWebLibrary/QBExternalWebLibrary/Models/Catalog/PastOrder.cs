using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models.Catalog;

public class PastOrder
{
    public int Id { get; set; }

    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [ForeignKey("Client")]
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public string? PONumber { get; set; }
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<PastOrderItem>? Items { get; set; }
    public List<PastOrderTag>? Tags { get; set; }
}
