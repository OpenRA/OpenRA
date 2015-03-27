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
		readonly Lazy<DeveloperMode> devMode;

		public TerrainGeometryOverlay(Actor self)
		{
			devMode = Exts.Lazy(() => self.World.LocalPlayer != null ? self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>() : null);
		}

		public void Render(WorldRenderer wr)
		{
			if (devMode.Value == null || !devMode.Value.ShowTerrainGeometry)
				return;

			var map = wr.World.Map;
			var tileSet = wr.World.TileSet;
			var lr = Game.Renderer.WorldLineRenderer;
			var colors = wr.World.TileSet.HeightDebugColors;
			var mouseCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos).ToMPos(wr.World.Map);

			foreach (var uv in wr.Viewport.VisibleCells.MapCoords)
			{
				var height = (int)map.MapHeight.Value[uv];
				var tile = map.MapTiles.Value[uv];
				var ti = tileSet.GetTileInfo(tile);
				var ramp = ti != null ? ti.RampType : 0;

				var corners = map.CellCorners[ramp];
				var color = corners.Select(c => colors[height + c.Z / 512]).ToArray();
				var pos = map.CenterOfCell(uv.ToCPos(map));
				var screen = corners.Select(c => wr.ScreenPxPosition(pos + c).ToFloat2()).ToArray();

				if (uv == mouseCell)
					lr.LineWidth = 3;

				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					lr.DrawLine(screen[i], screen[j], color[i], color[j]);
				}

				lr.LineWidth = 1;
			}
		}
	}
}
