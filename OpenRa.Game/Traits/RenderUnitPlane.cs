using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitPlane : RenderUnit
	{
		public RenderUnitPlane(Actor self)
			: base(self) {}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			yield return Util.CenteredShadow(self, anim.Image, self.CenterLocation);
			var p = self.CenterLocation - new float2(0, unit.Altitude);
			yield return Util.Centered(self, anim.Image, p);
		}
	}
}
