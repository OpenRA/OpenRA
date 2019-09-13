#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders a debug overlay showing the terrain cells. Attach this to the world actor.")]
	public class TerrainGeometryOverlayInfo : TraitInfo<TerrainGeometryOverlay> { }

	public class TerrainGeometryOverlay : IRenderAboveShroud, IWorldLoaded, IChatCommand
	{
		const string CommandName = "terrainoverlay";
		const string CommandDesc = "toggles the terrain geometry overlay.";

		public bool Enabled;

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDesc);
		}

		void IChatCommand.InvokeCommand(string name, string arg)
		{
			if (name == CommandName)
				Enabled ^= true;
		}

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			var map = wr.World.Map;
			var tileSet = wr.World.Map.Rules.TileSet;
			var colors = tileSet.HeightDebugColors;
			var mouseCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos).ToMPos(wr.World.Map);

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.Height.Contains(uv) || self.World.ShroudObscures(uv))
					continue;

				var height = (int)map.Height[uv];
				var tile = map.Tiles[uv];
				var ti = tileSet.GetTileInfo(tile);
				var ramp = ti != null ? ti.RampType : 0;

				var corners = map.Grid.CellCorners[ramp];
				var pos = map.CenterOfCell(uv.ToCPos(map));
				var width = uv == mouseCell ? 3 : 1;

				// Colors change between points, so render separately
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					var start = pos + corners[i];
					var end = pos + corners[j];
					var startColor = colors[height + corners[i].Z / 512];
					var endColor = colors[height + corners[j].Z / 512];
					yield return new LineAnnotationRenderable(start, end, width, startColor, endColor);
				}
			}

			// Projected cell coordinates for the current cell
			var projectedCorners = map.Grid.CellCorners[0];
			foreach (var puv in map.ProjectedCellsCovering(mouseCell))
			{
				var pos = map.CenterOfCell(((MPos)puv).ToCPos(map));
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					var start = pos + projectedCorners[i] - new WVec(0, 0, pos.Z);
					var end = pos + projectedCorners[j] - new WVec(0, 0, pos.Z);
					yield return new LineAnnotationRenderable(start, end, 3, Color.Navy);
				}
			}
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }
	}
}
