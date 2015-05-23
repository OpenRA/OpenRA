#region Copyright & License Information
/*
  * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders a debug overlay showing the terrain cells. Attach this to the world actor.")]
	public class TerrainGeometryOverlayInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new TerrainGeometryOverlay(init.Self); }
	}

	public class TerrainGeometryOverlay : IRenderOverlay
	{
		readonly int[][] vertices = new int[][]
		{
			// Flat
			new[] { 0, 0, 0, 0 },

			// Slopes (two corners high)
			new[] { 0, 0, 1, 1 },
			new[] { 1, 0, 0, 1 },
			new[] { 1, 1, 0, 0 },
			new[] { 0, 1, 1, 0 },

			// Slopes (one corner high)
			new[] { 0, 0, 0, 1 },
			new[] { 1, 0, 0, 0 },
			new[] { 0, 1, 0, 0 },
			new[] { 0, 0, 1, 0 },

			// Slopes (three corners high)
			new[] { 1, 0, 1, 1 },
			new[] { 1, 1, 0, 1 },
			new[] { 1, 1, 1, 0 },
			new[] { 0, 1, 1, 1 },

			// Slopes (two corners high, one corner double high)
			new[] { 1, 0, 1, 2 },
			new[] { 2, 1, 0, 1 },
			new[] { 1, 2, 1, 0 },
			new[] { 0, 1, 2, 1 },

			// Slopes (two corners high, alternating)
			new[] { 1, 0, 1, 0 },
			new[] { 0, 1, 0, 1 },
			new[] { 1, 0, 1, 0 },
			new[] { 0, 1, 0, 1 }
		};

		readonly Lazy<DeveloperMode> devMode;

		public TerrainGeometryOverlay(Actor self)
		{
			devMode = Exts.Lazy(() => self.World.LocalPlayer != null ? self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>() : null);
		}

		public void Render(WorldRenderer wr)
		{
			if (devMode.Value == null || !devMode.Value.ShowTerrainGeometry)
				return;

			var ts = wr.World.Map.TileShape;
			var colors = wr.World.TileSet.HeightDebugColors;

			var leftDelta = ts == TileShape.Diamond ? new WVec(-512, 0, 0) : new WVec(-512, -512, 0);
			var topDelta = ts == TileShape.Diamond ? new WVec(0, -512, 0) : new WVec(512, -512, 0);
			var rightDelta = ts == TileShape.Diamond ? new WVec(512, 0, 0) : new WVec(512, 512, 0);
			var bottomDelta = ts == TileShape.Diamond ? new WVec(0, 512, 0) : new WVec(-512, 512, 0);

			foreach (var uv in wr.Viewport.VisibleCells.MapCoords)
			{
				var lr = Game.Renderer.WorldLineRenderer;
				var pos = wr.World.Map.CenterOfCell(uv.ToCPos(wr.World.Map));

				var height = (int)wr.World.Map.MapHeight.Value[uv];
				var tile = wr.World.Map.MapTiles.Value[uv];

				TerrainTileInfo tileInfo = null;

				// TODO: This is a temporary workaround for our sloppy tileset definitions
				// (ra/td templates omit Clear tiles from templates)
				try
				{
					tileInfo = wr.World.TileSet.Templates[tile.Type][tile.Index];
				}
				catch (Exception) { }

				if (tileInfo == null)
					continue;

				var leftHeight = vertices[tileInfo.RampType][0];
				var topHeight = vertices[tileInfo.RampType][1];
				var rightHeight = vertices[tileInfo.RampType][2];
				var bottomHeight = vertices[tileInfo.RampType][3];

				var leftColor = colors[height + leftHeight];
				var topColor = colors[height + topHeight];
				var rightColor = colors[height + rightHeight];
				var bottomColor = colors[height + bottomHeight];

				var left = wr.ScreenPxPosition(pos + leftDelta + new WVec(0, 0, 512 * leftHeight)).ToFloat2();
				var top = wr.ScreenPxPosition(pos + topDelta + new WVec(0, 0, 512 * topHeight)).ToFloat2();
				var right = wr.ScreenPxPosition(pos + rightDelta + new WVec(0, 0, 512 * rightHeight)).ToFloat2();
				var bottom = wr.ScreenPxPosition(pos + bottomDelta + new WVec(0, 0, 512 * bottomHeight)).ToFloat2();

				lr.DrawLine(left, top, leftColor, topColor);
				lr.DrawLine(top, right, topColor, rightColor);
				lr.DrawLine(right, bottom, rightColor, bottomColor);
				lr.DrawLine(bottom, left, bottomColor, leftColor);
			}
		}
	}
}
