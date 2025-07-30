using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Models.Ariba
{
	public record AribaLoginRequest(string AribaId, string SharedSecret);
}
