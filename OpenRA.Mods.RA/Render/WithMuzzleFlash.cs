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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA;

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
			var render = self.Trait<RenderSimple>();
			var facing = self.TraitOrDefault<IFacing>();

			var arms = self.TraitsImplementing<Armament>();
			foreach (var a in arms)
				foreach(var b in a.Barrels)
				{
					var barrel = b;
					var turreted = self.TraitsImplementing<Turreted>()
						.FirstOrDefault(t => t.Name ==  a.Info.Turret);
					var getFacing = turreted != null ? () => turreted.turretFacing :
						facing != null ? (Func<int>)(() => facing.Facing) : () => 0;

					var muzzleFlash = new Animation(render.GetImage(self), getFacing);
					muzzleFlash.Play("muzzle");

					muzzleFlashes.Add("muzzle{0}".F(muzzleFlashes.Count), new AnimationWithOffset(
						muzzleFlash,
						wr => PPos.FromWPosHackZ(WPos.Zero + a.MuzzleOffset(self, barrel)).ToFloat2(),
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
					yield return a.Image(self, wr, wr.Palette("effect"));
		}

		public void Tick(Actor self)
		{
			foreach (var a in muzzleFlashes.Values)
				a.Animation.Tick();
		}
	}
}
