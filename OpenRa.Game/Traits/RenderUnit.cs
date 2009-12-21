using System;
using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class RenderUnit : RenderSimple, INotifyDamage
	{
		public RenderUnit(Actor self)
			: base(self)
		{
			PlayFacingAnim(self);

			anims.Add( "smoke", new AnimationWithOffset( new Animation( "smoke_m" ), null, () => !isSmoking ) );
		}

		void PlayFacingAnim(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			anim.PlayFacing("idle", () => unit.Facing);
		}

		public void PlayCustomAnimation(Actor self, string newAnim, Action after)
		{
			anim.PlayThen(newAnim, () => { PlayFacingAnim(self); if (after != null) after(); });
		}

		bool isSmoking;

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState != DamageState.Half) return;
			if (isSmoking) return;

			isSmoking = true;
			var smoke = anims[ "smoke" ].Animation;
			smoke.PlayThen( "idle",
				() => smoke.PlayThen( "loop",
					() => smoke.PlayBackwardsThen( "end",
						() => isSmoking = false ) ) );
		}
	}
}
