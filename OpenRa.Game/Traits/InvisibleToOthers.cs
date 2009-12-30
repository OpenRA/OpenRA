using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class InvisibleToOthers : IRenderModifier
	{
		public InvisibleToOthers(Actor self) { }

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return Game.LocalPlayer == self.Owner
				? r : new Renderable[] { };
		}
	}
}
