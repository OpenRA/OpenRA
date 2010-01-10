using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class IronCurtainInfo : ITraitInfo
	{
		public object Create(Actor self) { return new IronCurtain(self); }
	}

	class IronCurtain
	{
		public IronCurtain(Actor self) {}
	}
}
