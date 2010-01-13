using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class SelectableInfo : StatelessTraitInfo<Selectable>
	{
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		public readonly string Voice = "GenericVoice";
	}

	class Selectable {}
}
