using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class SelectableInfo : ITraitInfo
	{
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		public readonly string Voice = "GenericVoice";

		public object Create(Actor self) { return new Selectable(self); }
	}

	class Selectable
	{
		public Selectable( Actor self ) { }
	}
}
