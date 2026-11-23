#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("If present, limits the size height for all players.")]
	sealed class ViewportSizeLimitInfo : ConditionalTraitInfo
	{
		[Desc("The maximum viewport height for all players in world units.")]
		public readonly WDist AbsoluteHeight = WDist.FromCells(512);

		[Desc("The maximum viewport height for all players as a percentage of map height. Takes precedence over AbsoluteHeight if specified.")]
		public readonly int? PercentageHeight = null;

		public override object Create(ActorInitializer init) { return new ViewportSizeLimit(this); }
	}

	sealed class ViewportSizeLimit : ConditionalTrait<ViewportSizeLimitInfo>, IWorldLoaded
	{
		public ViewportSizeLimit(ViewportSizeLimitInfo info)
			: base(info) { }

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var height = Info.PercentageHeight.HasValue
				? WDist.FromCells(w.Map.Bounds.Height).Length * Info.PercentageHeight.Value / 100
				: Info.AbsoluteHeight.Length;

			// WPos to world pixels
			wr.Viewport.OverrideDefaultHeight(
					Math.Max(WDist.FromCells(1).Length, height) *
					w.Map.Rules.TerrainInfo.TileSize.Height /
					w.Map.Grid.TileScale);
		}
	}
}
