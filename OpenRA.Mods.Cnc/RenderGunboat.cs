#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderGunboatInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new RenderGunboat(init.self); }
	}

	class RenderGunboat : RenderSimple, INotifyDamageStateChanged
	{
		IFacing facing;
		string lastDir = "left";
		string lastDamage = "";

		public RenderGunboat(Actor self)
			: base(self, () => self.HasTrait<Turreted>() ? self.TraitsImplementing<Turreted>().First().turretFacing : 0)
		{
			facing = self.Trait<IFacing>();
			anim.Play("left");

			var wake = new Animation(anim.Name);
			wake.Play("left-wake");

			var leftOffset = new WVec(43, 86, 0);
			var rightOffset = new WVec(-43, 86, 0);
			anims.Add("wake", new AnimationWithOffset(wake,
				() => anims["wake"].Animation.CurrentSequence.Name == "left-wake" ? leftOffset : rightOffset,
			    () => false, -87));
		}

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
	}
}
