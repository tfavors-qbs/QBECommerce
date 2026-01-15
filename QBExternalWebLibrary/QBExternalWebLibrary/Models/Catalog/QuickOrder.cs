using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models.Catalog;

public class QuickOrder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ForeignKey("Owner")]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;

    [ForeignKey("Client")]
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public bool IsSharedClientWide { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public int TimesUsed { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<QuickOrderItem>? Items { get; set; }
    public List<QuickOrderTag>? Tags { get; set; }
}

public class QuickOrderEVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public bool IsSharedClientWide { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int TimesUsed { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsOwner { get; set; }
}
