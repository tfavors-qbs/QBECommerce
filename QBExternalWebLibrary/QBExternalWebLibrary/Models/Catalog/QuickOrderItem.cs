namespace QBExternalWebLibrary.Models.Catalog;

public class QuickOrderItem
{
    public int Id { get; set; }
    public int QuickOrderId { get; set; }
    public QuickOrder QuickOrder { get; set; } = null!;
    public int ContractItemId { get; set; }
    public ContractItem ContractItem { get; set; } = null!;
    public int Quantity { get; set; }
}

public class QuickOrderItemEVM
{
    public int Id { get; set; }
    public int QuickOrderId { get; set; }
    public int ContractItemId { get; set; }
    public ContractItemEditViewModel? ContractItem { get; set; }
    public int Quantity { get; set; }
    public bool IsAvailable { get; set; } = true;

    public QuickOrderItemEVM Copy()
    {
        return new QuickOrderItemEVM
        {
            Id = Id,
            QuickOrderId = QuickOrderId,
            ContractItemId = ContractItemId,
            ContractItem = ContractItem,
            Quantity = Quantity,
            IsAvailable = IsAvailable
        };
    }
}
