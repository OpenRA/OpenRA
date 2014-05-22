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
	public class WithSmokeInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public object Create(ActorInitializer init) { return new WithSmoke(init.self); }
	}

	public class WithSmoke : INotifyDamage
	{
		bool isSmoking;
		Animation anim;

		public WithSmoke(Actor self)
		{
			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, "smoke_m");
			rs.Add("smoke", new AnimationWithOffset(anim, null, () => !isSmoking));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (isSmoking) return;
			if (e.Damage < 0) return;	/* getting healed */
			if (e.DamageState < DamageState.Heavy) return;

			isSmoking = true;
			anim.PlayThen( "idle",
				() => anim.PlayThen( "loop",
					() => anim.PlayBackwardsThen( "end",
						() => isSmoking = false ) ) );
		}
	}
}
