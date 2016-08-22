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
using OpenRA.Mods.Common.Commands;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders a debug overlay showing the terrain cells. Attach this to the world actor.")]
	public class TerrainGeometryOverlayInfo : TraitInfo<TerrainGeometryOverlay> { }

	public class TerrainGeometryOverlay : IRenderAboveWorld, IWorldLoaded, IChatCommand
	{
		const string CommandName = "terrainoverlay";
		const string CommandDesc = "Toggles the terrain geometry overlay";

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

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				return;

			var map = wr.World.Map;
			var tileSet = wr.World.Map.Rules.TileSet;
			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			var colors = tileSet.HeightDebugColors;
			var mouseCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos).ToMPos(wr.World.Map);

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.Height.Contains(uv))
					continue;

				var height = (int)map.Height[uv];
				var tile = map.Tiles[uv];
				var ti = tileSet.GetTileInfo(tile);
				var ramp = ti != null ? ti.RampType : 0;

				var corners = map.Grid.CellCorners[ramp];
				var color = corners.Select(c => colors[height + c.Z / 512]).ToArray();
				var pos = map.CenterOfCell(uv.ToCPos(map));
				var screen = corners.Select(c => wr.Screen3DPxPosition(pos + c)).ToArray();
				var width = (uv == mouseCell ? 3 : 1) / wr.Viewport.Zoom;

				// Colors change between points, so render separately
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					wcr.DrawLine(screen[i], screen[j], width, color[i], color[j]);
				}
			}

			// Projected cell coordinates for the current cell
			var projectedCorners = map.Grid.CellCorners[0];
			foreach (var puv in map.ProjectedCellsCovering(mouseCell))
			{
				var pos = map.CenterOfCell(((MPos)puv).ToCPos(map));
				var screen = projectedCorners.Select(c => wr.Screen3DPxPosition(pos + c - new WVec(0, 0, pos.Z))).ToArray();
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					wcr.DrawLine(screen[i], screen[j], 3 / wr.Viewport.Zoom, Color.Navy);
				}
			}
		}
	}
}
