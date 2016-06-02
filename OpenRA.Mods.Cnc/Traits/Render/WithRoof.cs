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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("Provides an overlay for the Tiberian Dawn hover craft.")]
	public class WithRoofInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Sequence = "roof";

		public object Create(ActorInitializer init) { return new WithRoof(init.Self, this); }
	}

	public class WithRoof
	{
		public WithRoof(Actor self, WithRoofInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var roof = new Animation(self.World, rs.GetImage(self), () => self.Trait<IFacing>().Facing);
			roof.Play(info.Sequence);
			rs.Add(new AnimationWithOffset(roof, null, null, 1024));
		}
	}
}
