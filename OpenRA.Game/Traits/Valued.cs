using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	public class ValuedInfo : TraitInfo<Valued>
	{
		public readonly int Cost = 0;
	}

	public class TooltipInfo : TraitInfo<Tooltip>
	{
		public readonly string Description = "";
		public readonly string Name = "";
		public readonly string Icon = null;
		public readonly string[] AlternateName = { };
	}
	
	public class Valued { }
	public class Tooltip { }
}
