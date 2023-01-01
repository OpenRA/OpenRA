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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays `Exit` data for factories.")]
	public class ExitsDebugOverlayInfo : TraitInfo, Requires<ExitInfo>
	{
		[Desc("Should cell vectors be drawn for each perimeter cell?")]
		public readonly bool DrawPerimiterCellVectors = true;

		[Desc("Should cell vectors be drawn for each exit cell?")]
		public readonly bool DrawExitCellVectors = true;

		[Desc("Should lines be drawn for each exit (from spawn offset to the center of the exit cell)?")]
		public readonly bool DrawSpawnOffsetLines = true;

		public override object Create(ActorInitializer init) { return new ExitsDebugOverlay(init.Self, this); }
	}

	public class ExitsDebugOverlay : IRenderAnnotationsWhenSelected
	{
		readonly ExitsDebugOverlayManager manager;
		readonly ExitsDebugOverlayInfo info;
		readonly ExitInfo[] exits;

		CPos[] exitCells;
		WPos[] spawnPositions;
		CPos[] perimeterCells;

		public ExitsDebugOverlay(Actor self, ExitsDebugOverlayInfo info)
		{
			this.info = info;
			manager = self.World.WorldActor.TraitOrDefault<ExitsDebugOverlayManager>();
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (manager == null || !manager.Enabled)
				yield break;

			exitCells = exits.Select(e => self.Location + e.ExitCell).ToArray();

			if (info.DrawExitCellVectors)
			{
				foreach (var exitCell in exitCells)
				{
					var color = self.Owner.Color;
					var vec = exitCell - self.Location;
					var center = wr.World.Map.CenterOfCell(exitCell);
					yield return new TextAnnotationRenderable(manager.Font, center, 0, color, vec.ToString());
				}
			}

			if (info.DrawPerimiterCellVectors)
			{
				var occupiedCells = self.OccupiesSpace.OccupiedCells().Select(p => p.Cell).ToArray();
				perimeterCells = Util.ExpandFootprint(occupiedCells, true).Except(occupiedCells).ToArray();

				foreach (var perimCell in perimeterCells)
				{
					var color = Color.Gray;
					if (exitCells.Contains(perimCell))
						continue;

					var vec = perimCell - self.Location;
					var center = wr.World.Map.CenterOfCell(perimCell);
					yield return new TextAnnotationRenderable(manager.Font, center, 0, color, vec.ToString());
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
					yield return new LineAnnotationRenderable(spawnPos, exitCellCenter, 1, self.Owner.Color);
				}
			}
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => true;
	}
}
