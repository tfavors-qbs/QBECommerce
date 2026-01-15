using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models.Catalog;

public class PastOrderItem
{
    public int Id { get; set; }

    [ForeignKey("PastOrder")]
    public int PastOrderId { get; set; }
    public PastOrder PastOrder { get; set; } = null!;

    [ForeignKey("ContractItem")]
    public int ContractItemId { get; set; }
    public ContractItem ContractItem { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
