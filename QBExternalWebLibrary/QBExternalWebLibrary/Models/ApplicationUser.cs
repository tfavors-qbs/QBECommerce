using Microsoft.AspNetCore.Identity;
using QBExternalWebLibrary.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models {
    public class ApplicationUser : IdentityUser {
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public int? ClientId { get; set; }
        public Client? Client { get; set; }
        public bool IsDisabled { get; set; }
        public string? AribaId { get; set; }
    }
}
