using QBExternalWebLibrary.Models.Catalog;

namespace QBExternalWebLibrary.Models.Pages;

public class PastOrderPageEVM
{
    public List<PastOrderEVM> MyOrders { get; set; } = new();
    public List<PastOrderEVM> OrganizationOrders { get; set; } = new();
    public List<string> AllTags { get; set; } = new();
}
