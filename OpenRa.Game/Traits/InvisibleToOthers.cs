using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class InvisibleToOthersInfo : ITraitInfo
	{
		public object Create(Actor self) { return new InvisibleToOthers(self); }
	}

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
