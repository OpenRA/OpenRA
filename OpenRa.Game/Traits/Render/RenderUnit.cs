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
			: base(self, () => self.traits.Get<Unit>().Facing)
		{
			anim.Play("idle");

			anims.Add( "smoke", new AnimationWithOffset( new Animation( "smoke_m" ), null, () => !isSmoking ) );
		}

		public void PlayCustomAnimation(Actor self, string newAnim, Action after)
		{
			anim.PlayThen(newAnim, () => { anim.Play("idle"); if (after != null) after(); });
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
