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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Rendered together with an attack.")]
	public class WithAttackOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[FieldLoader.Require]
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = null;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

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

			renderSprites.Add(new AnimationWithOffset(overlay, null, () => !attacking),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			attacking = true;
			overlay.PlayThen(info.Sequence, () => attacking = false);
		}
	}
}