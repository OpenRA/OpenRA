#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders the MuzzleSequence from the Armament trait.")]
	class WithMuzzleOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<AttackBaseInfo>, Requires<ArmamentInfo>
	{
		[Desc("Ignore the weapon position, and always draw relative to the center of the actor")]
		public readonly bool IgnoreOffset = false;

		public override object Create(ActorInitializer init) { return new WithMuzzleOverlay(init.Self, this); }
	}

	class WithMuzzleOverlay : ConditionalTrait<WithMuzzleOverlayInfo>, INotifyAttack, IRender, ITick
	{
		readonly Dictionary<Barrel, bool> visible = new Dictionary<Barrel, bool>();
		readonly Dictionary<Barrel, AnimationWithOffset> anims = new Dictionary<Barrel, AnimationWithOffset>();
		readonly Func<WAngle> getFacing;
		readonly Armament[] armaments;

		public WithMuzzleOverlay(Actor self, WithMuzzleOverlayInfo info)
			: base(info)
		{
			var render = self.Trait<RenderSprites>();
			var facing = self.TraitOrDefault<IFacing>();

			armaments = self.TraitsImplementing<Armament>()
				.Where(arm => arm.Info.MuzzleSequence != null)
				.ToArray();

			foreach (var arm in armaments)
			{
				foreach (var b in arm.Barrels)
				{
					var barrel = b;
					var turreted = self.TraitsImplementing<Turreted>()
						.FirstOrDefault(t => t.Name == arm.Info.Turret);

					if (turreted != null)
						getFacing = () => turreted.WorldOrientation.Yaw;
					else if (facing != null)
						getFacing = () => facing.Facing;
					else
						getFacing = () => WAngle.Zero;

					var muzzleFlash = new Animation(self.World, render.GetImage(self), getFacing);
					visible.Add(barrel, false);
					anims.Add(barrel,
						new AnimationWithOffset(muzzleFlash,
							() => info.IgnoreOffset ? WVec.Zero : arm.MuzzleOffset(self, barrel),
							() => IsTraitDisabled || !visible[barrel],
							p => RenderUtils.ZOffsetFromCenter(self, p, 2)));
				}
			}
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (a == null || barrel == null || !armaments.Contains(a))
				return;

			var sequence = a.Info.MuzzleSequence;
			visible[barrel] = true;
			anims[barrel].Animation.PlayThen(sequence, () => visible[barrel] = false);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			foreach (var arm in armaments)
			{
				var palette = wr.Palette(arm.Info.MuzzlePalette);
				foreach (var b in arm.Barrels)
				{
					var anim = anims[b];
					if (anim.DisableFunc != null && anim.DisableFunc())
						continue;

					foreach (var r in anim.Render(self, wr, palette, 1f))
						yield return r;
				}
			}
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Muzzle flashes don't contribute to actor bounds
			yield break;
		}

		void ITick.Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Tick();
		}
	}
}
