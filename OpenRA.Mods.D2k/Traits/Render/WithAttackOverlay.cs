#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Rendered together with an attack.")]
	public class WithAttackOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = null;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithAttackOverlay(init, this); }
	}

	public class WithAttackOverlay : INotifyAttack
	{
		readonly Animation overlay;
		readonly RenderSprites renderSprites;
		readonly WithAttackOverlayInfo info;

		bool attacking;

		public WithAttackOverlay(ActorInitializer init, WithAttackOverlayInfo info)
		{
			this.info = info;

			renderSprites = init.Self.Trait<RenderSprites>();

			overlay = new Animation(init.World, renderSprites.GetImage(init.Self));

			var key = "attack_overlay_{0}".F(info.Sequence);
			renderSprites.Add(key, new AnimationWithOffset(overlay, null, () => !attacking),
				info.Palette, info.IsPlayerPalette);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			attacking = true;
			overlay.PlayThen(info.Sequence, () => attacking = false);
		}
	}
}