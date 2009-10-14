using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class RenderUnit : RenderSimple
	{
		public RenderUnit(Actor self)
			: base(self)
		{
			anim.PlayFetchIndex("idle", () => self.traits.Get<Mobile>().facing);
		}

		protected static Pair<Sprite, float2> Centered(Sprite s, float2 location)
		{
			var loc = location - 0.5f * s.size;
			return Pair.New(s, loc.Round());
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();
			float fraction = (mobile.moveFraction > 0) ? (float)mobile.moveFraction / mobile.moveFractionTotal : 0f;
			var centerLocation = new float2(12, 12) + Game.CellSize * float2.Lerp(mobile.fromCell, mobile.toCell, fraction);
			yield return Centered(anim.Image, centerLocation);
		}
	}
}
