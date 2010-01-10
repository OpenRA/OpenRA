using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class BuildableInfo : ITraitInfo
	{
		public readonly int TechLevel = -1;
		public readonly string Tab = null;
		public readonly string[] Prerequisites = { };
		public readonly Race[] Owner = { };
		public readonly int Cost = 0;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly string Icon = null;

		public object Create(Actor self) { return new Buildable(self); }
	}

	class Buildable
	{
		public Buildable( Actor self ) { }
	}
}
