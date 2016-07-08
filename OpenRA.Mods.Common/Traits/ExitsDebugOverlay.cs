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

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays `Exit` data for factories.")]
	public class ExitsDebugOverlayInfo : ITraitInfo, Requires<ExitInfo>
	{
		[Desc("Should cell vectors be drawn for each perimeter cell?")]
		public readonly bool DrawPerimiterCellVectors = true;

		[Desc("Should cell vectors be drawn for each exit cell?")]
		public readonly bool DrawExitCellVectors = true;

		[Desc("Should lines be drawn for each exit (from spawn offset to the center of the exit cell)?")]
		public readonly bool DrawSpawnOffsetLines = true;

		object ITraitInfo.Create(ActorInitializer init) { return new ExitsDebugOverlay(init.Self, this); }
	}

	public class ExitsDebugOverlay : IPostRender
	{
		readonly ExitsDebugOverlayManager manager;
		readonly ExitsDebugOverlayInfo info;
		readonly RgbaColorRenderer rgbaRenderer;
		readonly ExitInfo[] exits;

		CPos[] exitCells;
		WPos[] spawnPositions;
		CPos[] perimeterCells;

		public ExitsDebugOverlay(Actor self, ExitsDebugOverlayInfo info)
		{
			this.info = info;
			manager = self.World.WorldActor.TraitOrDefault<ExitsDebugOverlayManager>();
			rgbaRenderer = Game.Renderer.WorldRgbaColorRenderer;
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();
		}

		void IPostRender.RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (manager == null || !manager.Enabled)
				return;

			exitCells = exits.Select(e => self.Location + e.ExitCell).ToArray();

			if (info.DrawExitCellVectors)
			{
				foreach (var exitCell in exitCells)
				{
					var color = self.Owner.Color.RGB;
					var vec = exitCell - self.Location;
					var center = wr.World.Map.CenterOfCell(exitCell);
					new TextRenderable(manager.Font, center, 0, color, vec.ToString()).Render(wr);
				}
			}

			if (info.DrawPerimiterCellVectors)
			{
				var occupiedCells = self.OccupiesSpace.OccupiedCells().Select(p => p.First).ToArray();
				perimeterCells = Util.ExpandFootprint(occupiedCells, true).Except(occupiedCells).ToArray();

				foreach (var perimCell in perimeterCells)
				{
					var color = Color.Gray;
					if (exitCells.Contains(perimCell))
						continue;

					var vec = perimCell - self.Location;
					var center = wr.World.Map.CenterOfCell(perimCell);
					new TextRenderable(manager.Font, center, 0, color, vec.ToString()).Render(wr);
				}
			}

			if (info.DrawSpawnOffsetLines)
			{
				spawnPositions = exits.Select(e => self.CenterPosition + e.SpawnOffset).ToArray();

				for (var i = 0; i < spawnPositions.Length; i++)
				{
					var spawnPos = spawnPositions[i];
					if (spawnPos == self.CenterPosition)
						continue;

					var exitCellCenter = self.World.Map.CenterOfCell(exitCells[i]);
					rgbaRenderer.DrawLine(wr.ScreenPosition(spawnPos), wr.ScreenPosition(exitCellCenter), 1f, self.Owner.Color.RGB);
				}
			}
	    }
	}
}
