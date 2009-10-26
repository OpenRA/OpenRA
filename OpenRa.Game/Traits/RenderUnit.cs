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
			anim.PlayFetchIndex("idle", 
				() => self.traits.Get<Mobile>().facing 
					/ (256/anim.CurrentSequence.Length));
		}

		protected static Pair<Sprite, float2> Centered(Sprite s, float2 location)
		{
			var loc = location - 0.5f * s.size;
			return Pair.New(s, loc.Round());
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Centered( anim.Image, self.CenterLocation );
		}
	}
}
