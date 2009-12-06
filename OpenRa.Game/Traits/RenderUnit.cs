using System;
using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnit : RenderSimple, INotifyDamageEx
	{
		public RenderUnit(Actor self)
			: base(self)
		{
			PlayFacingAnim(self);
			smoke = new Animation("smoke_m");
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

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			yield return Util.Centered(self, anim.Image, self.CenterLocation);
			if (isSmoking)
				yield return Util.Centered(self, smoke.Image, self.CenterLocation);
		}

		bool isSmoking;
		DamageState currentDs;
		Animation smoke;

		public void Damaged(Actor self, DamageState ds) { currentDs = ds; }

		public void Damaged(Actor self, int damage)
		{
			if (currentDs != DamageState.Half) return;
			if (!isSmoking)
			{
				isSmoking = true;
				smoke.PlayThen("idle", 
					() => smoke.PlayThen("loop", 
						() => smoke.PlayBackwardsThen("end", 
							() => isSmoking = false)));
			}
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (isSmoking)
				smoke.Tick();
		}
	}
}
