using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http.ContentTypes.Identity {
    public class CustomRegisterRequest {
        public string email { get; set; }
        public string password { get; set; }
        public string givenName { get; set; }
        public string familyName { get; set; }

    }
}
