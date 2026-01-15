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

public class PastOrderEVM
{
    public int Id { get; set; }
    public string? PONumber { get; set; }
    public DateTime OrderedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public int? ClientId { get; set; }
}

public class PastOrderItemEVM
{
    public int Id { get; set; }
    public int PastOrderId { get; set; }
    public int ContractItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public bool IsAvailable { get; set; } = true;
}

public class PastOrderDetailEVM
{
    public PastOrderEVM Order { get; set; } = null!;
    public List<PastOrderItemEVM> Items { get; set; } = new();
}

public class ReorderResultEVM
{
    public List<PastOrderItemEVM> AddedItems { get; set; } = new();
    public List<PastOrderItemEVM> SkippedItems { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
