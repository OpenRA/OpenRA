#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders a sprite effect when leaving a cell.")]
	public class LeavesTrailsInfo : ITraitInfo
	{
		public readonly string Image = null;
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Only do so when the terrain types match with the previous cell.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new LeavesTrails(this, init.Self); }
	}

	public class LeavesTrails : ITick
	{
		readonly LeavesTrailsInfo info;

		public LeavesTrails(LeavesTrailsInfo info, Actor self)
		{
			this.info = info;
		}

		CPos cachedLocation;

		public void Tick(Actor self)
		{
			if (cachedLocation != self.Location)
			{
				var type = self.World.Map.GetTerrainInfo(cachedLocation).Type;
				var pos = self.World.Map.CenterOfCell(cachedLocation);
				if (info.TerrainTypes.Contains(type) && !string.IsNullOrEmpty(info.Image))
					self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, self.World, info.Image, info.Palette)));

				cachedLocation = self.Location;
			}
		}
	}
}
