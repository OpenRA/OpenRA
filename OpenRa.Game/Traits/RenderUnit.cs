using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

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
			anim.PlayFetchIndex("idle",
				() => Util.QuantizeFacing( 
					self.traits.Get<Mobile>().facing, 
					anim.CurrentSequence.Length ));
		}

		public void PlayCustomAnimation(Actor self, string newAnim, Action after)
		{
			anim.PlayThen(newAnim, () => { PlayFacingAnim(self); if (after != null) after(); });
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Util.Centered(anim.Image, self.CenterLocation);
			if (isSmoking)
				yield return Util.Centered(smoke.Image, self.CenterLocation);
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
