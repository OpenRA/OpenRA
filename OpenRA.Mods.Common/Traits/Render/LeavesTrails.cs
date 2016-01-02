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
	public enum TrailType { Cell, CenterPosition }

	[Desc("Renders a sprite effect when leaving a cell.")]
	public class LeavesTrailsInfo : ITraitInfo
	{
		public readonly string Image = null;
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Only do so when the terrain types match with the previous cell.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("Accepts values: Cell to draw the trail sprite in the center of the current cell,",
			"CenterPosition to draw the trail sprite at the current position.")]
		public readonly TrailType Type = TrailType.Cell;

		[Desc("Display a trail while stationary.")]
		public readonly bool TrailWhileStationary = false;

		[Desc("Delay between trail updates when stationary.")]
		public readonly int StationaryInterval = 0;

		[Desc("Display a trail while moving.")]
		public readonly bool TrailWhileMoving = true;

		[Desc("Delay between trail updates when moving.")]
		public readonly int MovingInterval = 0;

		public object Create(ActorInitializer init) { return new LeavesTrails(this, init.Self); }
	}

	public class LeavesTrails : ITick
	{
		readonly LeavesTrailsInfo info;

		public LeavesTrails(LeavesTrailsInfo info, Actor self)
		{
			this.info = info;
		}

		WPos cachedPosition;
		int ticks;

		public void Tick(Actor self)
		{
			var isMoving = self.CenterPosition != cachedPosition;
			if ((isMoving && !info.TrailWhileMoving) || (!isMoving && !info.TrailWhileStationary))
				return;

			var interval = isMoving ? info.MovingInterval :
				info.StationaryInterval;
			if (++ticks >= interval)
			{
				var cachedCell = self.World.Map.CellContaining(cachedPosition);
				var type = self.World.Map.GetTerrainInfo(cachedCell).Type;

				var pos = info.Type == TrailType.CenterPosition ? cachedPosition :
					self.World.Map.CenterOfCell(cachedCell);

				if (info.TerrainTypes.Contains(type) && !string.IsNullOrEmpty(info.Image))
					self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, self.World, info.Image, "idle", info.Palette)));

				cachedPosition = self.CenterPosition;
				ticks = 0;
			}
		}
	}
}
