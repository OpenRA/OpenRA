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
			PlayFacingAnim(self);
		}

		void PlayFacingAnim(Actor self)
		{
			anim.PlayFetchIndex("idle",
				() => self.traits.Get<Mobile>().facing
					/ (256 / anim.CurrentSequence.Length));
		}

		public void PlayCustomAnimation(Actor self, string newAnim, Action after)
		{
			anim.PlayThen(newAnim, () => { PlayFacingAnim(self); if (after != null) after(); });
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Util.Centered(anim.Image, self.CenterLocation);
		}
	}
}
