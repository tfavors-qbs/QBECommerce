using System.ComponentModel.DataAnnotations.Schema;

namespace QBExternalWebLibrary.Models.Catalog;

public class PastOrderTag
{
    public int Id { get; set; }

    [ForeignKey("PastOrder")]
    public int PastOrderId { get; set; }
    public PastOrder PastOrder { get; set; } = null!;

    public string Tag { get; set; } = string.Empty;
}
