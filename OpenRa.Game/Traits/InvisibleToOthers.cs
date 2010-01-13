using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class InvisibleToOthersInfo : StatelessTraitInfo<InvisibleToOthers> { }

	class InvisibleToOthers : IRenderModifier
	{
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return Game.LocalPlayer == self.Owner
				? r : new Renderable[] { };
		}
	}
}
