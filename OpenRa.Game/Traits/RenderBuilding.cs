using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderBuilding : RenderSimple
	{
		public RenderBuilding(Actor self)
			: base(self)
		{
			anim.PlayThen("make", () => anim.PlayRepeating("idle"));
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Pair.New(anim.Image, 24f * (float2)self.Location);
		}
	}
}
