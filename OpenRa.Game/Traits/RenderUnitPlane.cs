using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class RenderUnitPlane : RenderUnit
	{
		Animation debug = new Animation("litning");
		public float2[] wps;

		public RenderUnitPlane(Actor self)
			: base(self) { debug.PlayRepeating("bright"); debug.Tick(); }

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var unit = self.traits.Get<Unit>();

			yield return Util.CenteredShadow(self, anim.Image, self.CenterLocation);
			var p = self.CenterLocation - new float2(0, unit.Altitude);
			yield return Util.Centered(self, anim.Image, p);

			if (wps != null)
				foreach (var w in wps)
					yield return Tuple.New(debug.Image, w - .5f * debug.Image.size, 0);
		}
	}
}
