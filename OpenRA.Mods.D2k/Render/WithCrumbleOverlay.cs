#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Render
{
	public class WithCrumbleOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "crumble-overlay";

		public object Create(ActorInitializer init) { return new WithCrumbleOverlay(init.self, this); }
	}

	public class WithCrumbleOverlay : INotifyBuildComplete
	{
		Animation overlay;
		bool buildComplete = false;

		public WithCrumbleOverlay(Actor self, WithCrumbleOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();

			overlay = new Animation(rs.GetImage(self));
			overlay.Play(info.Sequence);
			rs.anims.Add("make_overlay_{0}".F(info.Sequence), 
				new AnimationWithOffset(overlay, null, () => !buildComplete, null));
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}
	}
}