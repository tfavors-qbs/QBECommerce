namespace QBExternalWebLibrary.Models.Catalog;

public class QuickOrderTag
{
    public int Id { get; set; }
    public int QuickOrderId { get; set; }
    public QuickOrder QuickOrder { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}
