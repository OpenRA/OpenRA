using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits.Render
{
	class HiddenUnderFogInfo : ITraitInfo
	{
		public object Create(Actor self) { return new HiddenUnderFog(self); }
	}

	class HiddenUnderFog : IRenderModifier
	{
		Shroud shroud;

		public HiddenUnderFog(Actor self)
		{
			shroud = self.World.WorldActor.traits.Get<Shroud>();
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			if (self.Owner == self.World.LocalPlayer || shroud.visibleCells[self.Location.X, self.Location.Y] > 0)
				return r;

			return new Renderable[] { };
		}
	}
}
