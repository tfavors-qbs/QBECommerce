using System.ComponentModel;

namespace QBExternalWebLibrary.Models.Products
{
    public class Group
    {
        public int Id { get; set; }
        [DisplayName("Legacy ID")]
        public string LegacyId { get; set; }
        public string Name { get; set; }
        [DisplayName("Display Name")]
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public int ClassId { get; set; }
        public Class Class { get; set; }
    }

    public class GroupEditViewModel
    {
        public int Id { get; set; }
        [DisplayName("Legacy ID")]
        public string LegacyId { get; set; }
        public string Name { get; set; }
        [DisplayName("Display Name")]
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public int ClassId { get; set; }
    }
}
