#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithCrumbleOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "crumble-overlay";

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithCrumbleOverlay(init, this); }
	}

	public class WithCrumbleOverlay : INotifyBuildComplete
	{
		bool buildComplete = false;

		public WithCrumbleOverlay(ActorInitializer init, WithCrumbleOverlayInfo info)
		{
			var rs = init.self.Trait<RenderSprites>();

			if (!init.Contains<SkipMakeAnimsInit>())
			{
				var overlay = new Animation(init.world, rs.GetImage(init.self));
				overlay.PlayThen(info.Sequence, () => buildComplete = false);
				rs.Add("make_overlay_{0}".F(info.Sequence),
					new AnimationWithOffset(overlay, null, () => !buildComplete),
					info.Palette, info.IsPlayerPalette);
			}
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}
	}
}