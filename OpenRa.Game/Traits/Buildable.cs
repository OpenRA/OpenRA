using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class BuildableInfo : StatelessTraitInfo<Buildable>
	{
		public readonly int TechLevel = -1;
		public readonly string Tab = null;
		public readonly string[] Prerequisites = { };
		public readonly string[] BuiltAt = { };
		public readonly Race[] Owner = { };
		public readonly int Cost = 0;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly string Icon = null;
		public readonly string[] AlternateName = { };
	}

	class Buildable { }
}
