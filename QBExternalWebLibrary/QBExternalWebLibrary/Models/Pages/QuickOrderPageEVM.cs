using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Pages;

public class QuickOrderPageEVM
{
    public List<QuickOrderEVM> MyQuickOrders { get; set; } = new();
    public List<QuickOrderEVM> SharedQuickOrders { get; set; } = new();
    public List<string> AllTags { get; set; } = new();
}

public class QuickOrderDetailEVM
{
    public QuickOrderEVM QuickOrder { get; set; } = null!;
    public List<QuickOrderItemEVM> Items { get; set; } = new();
    public List<string> AvailableTags { get; set; } = new();
}
