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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Rendered together with an attack.")]
	public class WithAttackOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[FieldLoader.Require]
		[Desc("Sequence name to use")]
		public readonly string Sequence = null;

		[PaletteReference("IsPlayerPalette")]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Delay in ticks before overlay starts, either relative to attack preparation or attack.")]
		public readonly int Delay = 0;

		[Desc("Should the overlay be delayed relative to preparation or actual attack?")]
		public readonly AttackDelayType DelayRelativeTo = AttackDelayType.Preparation;

		public override object Create(ActorInitializer init) { return new WithAttackOverlay(init, this); }
	}

	public class WithAttackOverlay : INotifyAttack, ITick
	{
		readonly Animation overlay;
		readonly RenderSprites renderSprites;
		readonly WithAttackOverlayInfo info;

		bool attacking;
		int tick;

		public WithAttackOverlay(ActorInitializer init, WithAttackOverlayInfo info)
		{
			this.info = info;

			renderSprites = init.Self.Trait<RenderSprites>();

			overlay = new Animation(init.World, renderSprites.GetImage(init.Self));

			renderSprites.Add(new AnimationWithOffset(overlay, null, () => !attacking),
				info.Palette, info.IsPlayerPalette);
		}

		void PlayOverlay(Actor self)
		{
			attacking = true;
			overlay.PlayThen(info.Sequence, () => attacking = false);
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (info.DelayRelativeTo == AttackDelayType.Attack)
			{
				if (info.Delay > 0)
					tick = info.Delay;
				else
					PlayOverlay(self);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (info.DelayRelativeTo == AttackDelayType.Preparation)
			{
				if (info.Delay > 0)
					tick = info.Delay;
				else
					PlayOverlay(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (info.Delay > 0 && --tick == 0)
				PlayOverlay(self);
		}
	}
}
