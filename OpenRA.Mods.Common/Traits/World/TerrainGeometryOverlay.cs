#region Copyright & License Information
/*
  * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders a debug overlay showing the terrain cells. Attach this to the world actor.")]
	public class TerrainGeometryOverlayInfo : TraitInfo<TerrainGeometryOverlay> { }

	public class TerrainGeometryOverlay : IPostRender, IWorldLoaded, IChatCommand
	{
		const string CommandName = "terrainoverlay";
		const string CommandDesc = "Toggles the terrain geometry overlay";

		public bool Enabled;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDesc);
		}

		public void InvokeCommand(string name, string arg)
		{
			if (name == CommandName)
				Enabled ^= true;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (!Enabled)
				return;

			var map = wr.World.Map;
			var tileSet = wr.World.TileSet;
			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			var colors = wr.World.TileSet.HeightDebugColors;
			var mouseCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos).ToMPos(wr.World.Map);

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.MapHeight.Value.Contains(uv))
					continue;

				var height = (int)map.MapHeight.Value[uv];
				var tile = map.MapTiles.Value[uv];
				var ti = tileSet.GetTileInfo(tile);
				var ramp = ti != null ? (int)ti.RampType : 0;

				var corners = map.CellCorners[ramp];
				var color = corners.Select(c => colors[height + c.Z / 512]).ToArray();
				var pos = map.CenterOfCell(uv.ToCPos(map));
				var screen = corners.Select(c => wr.ScreenPxPosition(pos + c).ToFloat2()).ToArray();
				var width = (uv == mouseCell ? 3 : 1) / wr.Viewport.Zoom;

				// Colors change between points, so render separately
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					wcr.DrawLine(screen[i], screen[j], width, color[i], color[j]);
				}
			}

			// Projected cell coordinates for the current cell
			var projectedCorners = map.CellCorners[0];
			foreach (var puv in map.ProjectedCellsCovering(mouseCell))
			{
				var pos = map.CenterOfCell(((MPos)puv).ToCPos(map));
				var screen = projectedCorners.Select(c => wr.ScreenPxPosition(pos + c - new WVec(0, 0, pos.Z)).ToFloat2()).ToArray();
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					wcr.DrawLine(screen[i], screen[j], 3 / wr.Viewport.Zoom, Color.Navy);
				}
			}
		}
	}
}
