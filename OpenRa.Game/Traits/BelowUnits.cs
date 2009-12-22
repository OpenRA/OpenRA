using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class BelowUnits : IRenderModifier
	{
		public BelowUnits(Actor self) { }

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return r.Select(a => a.WithZOffset(-1));
		}
	}
}
