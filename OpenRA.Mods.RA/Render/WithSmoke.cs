#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithSmokeInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Needs to define \"idle\", \"loop\" and \"end\" sub-sequences.")]
		public readonly string Sequence = "smoke_m";

		public object Create(ActorInitializer init) { return new WithSmoke(init.self, this); }
	}

	public class WithSmoke : INotifyDamage
	{
		bool isSmoking;
		Animation anim;

		public WithSmoke(Actor self, WithSmokeInfo info)
		{
			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, info.Sequence);
			rs.Add("smoke", new AnimationWithOffset(anim, null, () => !isSmoking));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (isSmoking) return;
			if (e.Damage < 0) return;	/* getting healed */
			if (e.DamageState < DamageState.Heavy) return;

			isSmoking = true;
			anim.PlayThen("idle",
				() => anim.PlayThen("loop",
					() => anim.PlayBackwardsThen("end",
						() => isSmoking = false)));
		}
	}
}
