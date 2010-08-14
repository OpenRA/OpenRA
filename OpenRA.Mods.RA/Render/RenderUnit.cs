#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderUnitInfo : RenderSimpleInfo
	{
		public override object Create(ActorInitializer init) { return new RenderUnit(init.self); }
	}

	public class RenderUnit : RenderSimple, INotifyDamage
	{
		public RenderUnit(Actor self)
			: base(self, () => self.HasTrait<IFacing>() ? self.Trait<IFacing>().Facing : 0)
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
			if (e.DamageState < DamageState.Heavy) return;
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
