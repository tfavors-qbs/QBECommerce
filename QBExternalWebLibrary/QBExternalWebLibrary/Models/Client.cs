using System.ComponentModel;

namespace QBExternalWebLibrary.Models {
    public class Client {
        public int Id { get; set; }
        [DisplayName("Legacy ID")]
        public string LegacyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<ApplicationUser> Users { get; set; }
        public ICollection<ContractItem> ContractItems { get; set; } = new List<ContractItem>();
    }

    public class ClientEditViewModel {
        public int Id { get; set; }
        [DisplayName("Legacy ID")]
        public string LegacyId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}