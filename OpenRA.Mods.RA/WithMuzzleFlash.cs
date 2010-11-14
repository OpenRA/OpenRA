#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class WithMuzzleFlashInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public object Create(ActorInitializer init) { return new WithMuzzleFlash(init.self); }
	}

	class WithMuzzleFlash : INotifyAttack
	{
		List<Animation> muzzleFlashes = new List<Animation>();
		bool isShowing;

		public WithMuzzleFlash(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			var render = self.Trait<RenderSimple>();
			var facing = self.Trait<IFacing>();

			foreach (var t in attack.Turrets)
			{
				var turret = t;
				var muzzleFlash = new Animation(render.GetImage(self), () => self.Trait<IFacing>().Facing);
				muzzleFlash.Play("muzzle");

				render.anims.Add("muzzle{0}".F(muzzleFlashes.Count), new RenderSimple.AnimationWithOffset(
					muzzleFlash,
					() => Combat.GetTurretPosition(self, facing, turret),
					() => !isShowing));

				muzzleFlashes.Add(muzzleFlash);
			}
		}

		public void Attacking(Actor self, Target target)
		{
			isShowing = true;
			foreach( var mf in muzzleFlashes )
				mf.PlayThen("muzzle", () => isShowing = false);
		}
	}
}
