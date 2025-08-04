using Ariba;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBExternalWebLibrary.Utility
{
	public static class Utility
	{
		public static T? ParseEnumAsNullable<T>(string operationString) where T : struct , Enum
		{
			return Enum.TryParse<T>(operationString, out var result) ? result : null;
		}
	}
}
