using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderSpyInfo : ITraitInfo
	{
		public object Create(Actor self) { return new RenderSpy(self); }
	}

	class RenderSpy : RenderInfantry, IRenderModifier
	{
		public RenderSpy(Actor self)
			: base(self)
		{
			if (self.Owner != Game.LocalPlayer)
				anim = new Animation("e1");
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			if (self.Owner == Game.LocalPlayer)
				return r;

			return r.Select(a => a.WithPalette(Game.LocalPlayer.Palette));
		}
	}
}
