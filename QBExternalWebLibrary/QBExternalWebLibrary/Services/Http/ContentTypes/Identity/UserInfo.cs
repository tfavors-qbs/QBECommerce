using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http.ContentTypes.Identity {
    /// <summary>
    /// User info from identity endpoint to establish claims.
    /// </summary>
    public class UserInfo {
        /// <summary>
        /// The email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Given name.
        /// </summary>
        public string? GivenName { get; set;}

        /// <summary>
        /// Family name.
        /// </summary>
        public string? FamilyName { get;set;}

        /// <summary>
        /// Client Id.
        /// </summary>
        public int? ClientId { get; set; } = default;

        /// <summary>
        /// Client name.
        /// </summary>
        public string? ClientName { get; set; } = string.Empty;

        /// <summary>
        /// A value indicating whether the email has been confirmed yet.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        /// The list of roles assigned to the user.
        /// </summary>
        public List<string> Roles { get; set; } = [];

        /// <summary>
        /// The list of claims for the user.
        /// </summary>
        public Dictionary<string, string> Claims { get; set; } = [];
    }
}
