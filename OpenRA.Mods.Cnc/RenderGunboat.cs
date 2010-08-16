#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderGunboatInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new RenderGunboat(init.self); }
	}

	class RenderGunboat : RenderSimple, INotifyDamage
	{
		IFacing facing;
		public RenderGunboat(Actor self)
			: base(self, () => self.HasTrait<Turreted>() ? self.Trait<Turreted>().turretFacing : 0)
		{
			facing = self.Trait<IFacing>();
			
			anim.Play("left");
			anims.Add( "smoke", new AnimationWithOffset( new Animation( "smoke_m" ), null, () => !isSmoking ) );
		}

		string lastDir = "left";
		string lastDamage = "";
		public override void Tick(Actor self)
		{
			var dir = (facing.Facing > 128) ? "right" : "left";
			if (dir != lastDir)
				anim.ReplaceAnim((lastDir = dir)+lastDamage);

			base.Tick(self);
		}
		
		bool isSmoking;
		public void Damaged(Actor self, AttackInfo e)
		{
			// Damagestate
			if (e.DamageStateChanged)
			{			
				if (e.DamageState >= DamageState.Critical)
					lastDamage = "-critical";
				else if (e.DamageState >= DamageState.Heavy)
					lastDamage = "-damaged";
				else if (e.DamageState < DamageState.Heavy)
					lastDamage = "";
				anim.ReplaceAnim(lastDir+lastDamage);
			}
			
			// Smoking
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
