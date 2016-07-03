#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders the MuzzleSequence from the Armament trait.")]
	class WithMuzzleOverlayInfo : UpgradableTraitInfo, Requires<RenderSpritesInfo>, Requires<AttackBaseInfo>, Requires<ArmamentInfo>
	{
		[Desc("Ignore the weapon position, and always draw relative to the center of the actor")]
		public readonly bool IgnoreOffset = false;

		public override object Create(ActorInitializer init) { return new WithMuzzleOverlay(init.Self, this); }
	}

	class WithMuzzleOverlay : UpgradableTrait<WithMuzzleOverlayInfo>, INotifyAttack, IRender, ITick
	{
		readonly Dictionary<Barrel, bool> visible = new Dictionary<Barrel, bool>();
		readonly Dictionary<Barrel, AnimationWithOffset> anims = new Dictionary<Barrel, AnimationWithOffset>();
		readonly Func<int> getFacing;
		readonly Armament[] armaments;

		public WithMuzzleOverlay(Actor self, WithMuzzleOverlayInfo info)
			: base(info)
		{
			var render = self.Trait<RenderSprites>();
			var facing = self.TraitOrDefault<IFacing>();

			armaments = self.TraitsImplementing<Armament>().ToArray();

			foreach (var arm in armaments)
			{
				var armClosure = arm;	// closure hazard in AnimationWithOffset

				// Skip armaments that don't define muzzles
				if (arm.Info.MuzzleSequence == null)
					continue;

				foreach (var b in arm.Barrels)
				{
					var barrel = b;
					var turreted = self.TraitsImplementing<Turreted>()
						.FirstOrDefault(t => t.Name == arm.Info.Turret);

					// Workaround for broken ternary operators in certain versions of mono (3.10 and
					// certain versions of the 3.8 series): https://bugzilla.xamarin.com/show_bug.cgi?id=23319
					if (turreted != null)
						getFacing = () => turreted.TurretFacing;
					else if (facing != null)
						getFacing = () => facing.Facing;
					else
						getFacing = () => 0;

					var muzzleFlash = new Animation(self.World, render.GetImage(self), getFacing);
					visible.Add(barrel, false);
					anims.Add(barrel,
						new AnimationWithOffset(muzzleFlash,
							() => info.IgnoreOffset ? WVec.Zero : armClosure.MuzzleOffset(self, barrel),
							() => IsTraitDisabled || !visible[barrel],
							p => RenderUtils.ZOffsetFromCenter(self, p, 2)));
				}
			}
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a == null)
				return;

			var sequence = a.Info.MuzzleSequence;
			if (sequence == null)
				return;

			if (a.Info.MuzzleSplitFacings > 0)
				sequence += Util.QuantizeFacing(getFacing(), a.Info.MuzzleSplitFacings).ToString();

			if (barrel == null)
				return;

			visible[barrel] = true;
			anims[barrel].Animation.PlayThen(sequence, () => visible[barrel] = false);
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			foreach (var arm in armaments)
			{
				var palette = wr.Palette(arm.Info.MuzzlePalette);
				foreach (var kv in anims)
				{
					if (!visible[kv.Key])
						continue;

					if (kv.Value.DisableFunc != null && kv.Value.DisableFunc())
						continue;

					foreach (var r in kv.Value.Render(self, wr, palette, 1f))
						yield return r;
				}
			}
		}

		public void Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Tick();
		}
	}
}
