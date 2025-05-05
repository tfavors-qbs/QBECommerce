using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Services.Http.ContentTypes.Identity {
    public class CustomLoginRequest {
        public string email { get; set; }
        public string password { get; set; }
    }
}
