using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderSubmarine : RenderSimple, INotifyAttack, ITick
	{
		int remainingSurfaceTime = 2;		/* setup for initial dive */

		public RenderSubmarine(Actor self)
			: base(self)
		{
			anim.PlayFacing("idle", () => self.traits.Get<Unit>().Facing);
		}

		public void Attacking(Actor self)
		{
			if (remainingSurfaceTime <= 0)
				OnSurface();

			remainingSurfaceTime = (int)(Rules.General.SubmergeDelay * 60 / 25);
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var s = Util.Centered(self, anim.Image, self.CenterLocation);
			if (remainingSurfaceTime <= 0)
			{
				s.c = 8;	/* shadow only palette */
				if (self.Owner != Game.LocalPlayer)
					yield break;	/* can't see someone else's submerged subs */
			}
			yield return s;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (remainingSurfaceTime > 0)
				if (--remainingSurfaceTime <= 0)
					OnDive();
		}

		void OnSurface()
		{
			Sound.Play("subshow1.aud");
		}

		void OnDive()
		{
			Sound.Play("subshow1.aud");		/* is this the right sound?? */
		}
	}
}
