#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;
using System;

namespace OpenRA.Mods.RA.Render
{
	class WithMuzzleFlashInfo : ITraitInfo, Requires<RenderSimpleInfo>, Requires<AttackBaseInfo>
	{
		public object Create(ActorInitializer init) { return new WithMuzzleFlash(init.self); }
	}

	class WithMuzzleFlash : INotifyAttack, IRender, ITick
	{
		Dictionary<string, AnimationWithOffset> muzzleFlashes = new Dictionary<string, AnimationWithOffset>();
		bool isShowing;

		public WithMuzzleFlash(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			var render = self.Trait<RenderSimple>();
			var facing = self.TraitOrDefault<IFacing>();
			var turreted = self.TraitOrDefault<Turreted>();
			var getFacing = turreted != null ? () => turreted.turretFacing :
							facing != null ? (Func<int>)(() => facing.Facing) : () => 0;

			foreach (var w in attack.Weapons)
				foreach( var b in w.Barrels )
				{
					var barrel = b;
					var turret = w.Turret;

					var muzzleFlash = new Animation(render.GetImage(self), getFacing);
					muzzleFlash.Play("muzzle");

					muzzleFlashes.Add("muzzle{0}".F(muzzleFlashes.Count), new AnimationWithOffset(
						muzzleFlash,
						() => Combat.GetBarrelPosition(self, facing, turret, barrel).ToFloat2(),
						() => !isShowing));
				}
		}

		public void Attacking(Actor self, Target target)
		{
			isShowing = true;
			foreach( var mf in muzzleFlashes.Values )
				mf.Animation.PlayThen("muzzle", () => isShowing = false);
		}

		public IEnumerable<Renderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var a in muzzleFlashes.Values)
				if (a.DisableFunc == null || !a.DisableFunc())
					yield return a.Image(self, wr.Palette("effect"));
		}

		public void Tick(Actor self)
		{
			foreach (var a in muzzleFlashes.Values)
				a.Animation.Tick();
		}
	}
}
