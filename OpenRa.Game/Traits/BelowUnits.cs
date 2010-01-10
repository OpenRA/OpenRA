using System.Collections.Generic;
using System.Linq;

namespace OpenRa.Game.Traits
{
	class BelowUnitsInfo : StatelessTraitInfo<BelowUnits> { }

	class BelowUnits : IRenderModifier
	{
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return r.Select(a => a.WithZOffset(-1));
		}
	}
}
