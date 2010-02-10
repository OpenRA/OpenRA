using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	public class CountryInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Race = null;

		/* todo: icon,... */

		public object Create(Actor self) { return new CountryInfo(); }
	}

	class Country { /* we're only interested in the Info */ }
}
