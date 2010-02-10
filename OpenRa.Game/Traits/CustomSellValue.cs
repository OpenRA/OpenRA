using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	// allow a nonstandard sell/repair value to avoid
	// buy-sell exploits like c&c's PROC.

	class CustomSellValueInfo : StatelessTraitInfo<CustomSellValue>
	{
		public readonly int Value = 0;
	}

	class CustomSellValue {}
}
