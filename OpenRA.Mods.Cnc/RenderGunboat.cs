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
using System;

namespace OpenRA.Mods.RA.Render
{
	class RenderGunboatInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new RenderGunboat(init.self); }
	}

	class RenderGunboat : RenderSimple, INotifyDamage, INotifyDamageStateChanged
	{
		IFacing facing;
		bool isSmoking = false;

		public RenderGunboat(Actor self)
			: base(self, () => self.HasTrait<Turreted>() ? self.Trait<Turreted>().turretFacing : 0)
		{
			facing = self.Trait<IFacing>();
			anim.Play("left");

			var wake = new Animation(anim.Name);
			wake.Play("left-wake");
			Func<float2> offset = () => new float2(((anims["wake"].Animation.CurrentSequence.Name == "left-wake") ? 1 : -1),2);
			anims.Add( "wake", new AnimationWithOffset( wake, offset, () => false ) { ZOffset = -2 } );
			anims.Add( "smoke", new AnimationWithOffset( new Animation( "smoke_m" ), null, () => !isSmoking ) );
		}

		string lastDir = "left";
		string lastDamage = "";
		public override void Tick(Actor self)
		{
			var dir = (facing.Facing > 128) ? "right" : "left";
			if (dir != lastDir)
			{
				anim.ReplaceAnim(dir+lastDamage);
				anims["wake"].Animation.ReplaceAnim(dir+"-wake");
				lastDir = dir;
			}
			base.Tick(self);
		}
		
		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (e.DamageState >= DamageState.Critical)
				lastDamage = "-critical";
			else if (e.DamageState >= DamageState.Heavy)
				lastDamage = "-damaged";
			else if (e.DamageState < DamageState.Heavy)
				lastDamage = "";
			anim.ReplaceAnim(lastDir+lastDamage);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
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
