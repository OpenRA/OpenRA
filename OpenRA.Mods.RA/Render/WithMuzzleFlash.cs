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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class WithMuzzleFlashInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<AttackBaseInfo>, Requires<ArmamentInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "muzzle";

		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Are the muzzle facings split into multiple shps?")]
		public readonly bool SplitFacings = false;

		[Desc("Number of separate facing images that are defined in the sequences.")]
		public readonly int FacingCount = 8;

		public object Create(ActorInitializer init) { return new WithMuzzleFlash(init.self, this); }
	}

	class WithMuzzleFlash : INotifyAttack, IRender, ITick
	{
		readonly WithMuzzleFlashInfo info;
		Dictionary<Barrel, bool> visible = new Dictionary<Barrel, bool>();
		Dictionary<Barrel, AnimationWithOffset> anims = new Dictionary<Barrel, AnimationWithOffset>();
		Func<int> getFacing;

		public WithMuzzleFlash(Actor self, WithMuzzleFlashInfo info)
		{
			this.info = info;
			var render = self.Trait<RenderSprites>();
			var facing = self.TraitOrDefault<IFacing>();

			var arm = self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == info.Armament);

			foreach (var b in arm.Barrels)
			{
				var barrel = b;
				var turreted = self.TraitsImplementing<Turreted>()
					.FirstOrDefault(t => t.Name ==  arm.Info.Turret);

				getFacing = turreted != null ? () => turreted.turretFacing :
					facing != null ? (Func<int>)(() => facing.Facing) : () => 0;

				var muzzleFlash = new Animation(render.GetImage(self), getFacing);
				visible.Add(barrel, false);
				anims.Add(barrel,
			    	new AnimationWithOffset(muzzleFlash,
						() => arm.MuzzleOffset(self, barrel),
						() => !visible[barrel],
						p => WithTurret.ZOffsetFromCenter(self, p, 2)));
			}
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != info.Armament)
				return;

			visible[barrel] = true;
			var sequence = info.Sequence;

			if (info.SplitFacings)
				sequence += Traits.Util.QuantizeFacing(getFacing(), info.FacingCount).ToString();

			anims[barrel].Animation.PlayThen(sequence, () => visible[barrel] = false);
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var kv in anims)
			{
				if (!visible[kv.Key])
					continue;

				if (kv.Value.DisableFunc != null && kv.Value.DisableFunc())
					continue;

				foreach (var r in kv.Value.Render(self, wr, wr.Palette("effect"), 1f))
					yield return r;
			}
		}

		public void Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Tick();
		}
	}
}
