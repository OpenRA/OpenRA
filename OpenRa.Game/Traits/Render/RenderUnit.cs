using System;
using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.GameRules;

namespace OpenRa.Traits
{
	class RenderUnitInfo : RenderSimpleInfo
	{
		public override object Create(Actor self) { return new RenderUnit(self); }
	}

	class RenderUnit : RenderSimple, INotifyDamage
	{
		public RenderUnit(Actor self)
			: base(self)
		{
			anim = new Animation( GetImage( self ), () => self.traits.Get<Unit>().Facing );
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
