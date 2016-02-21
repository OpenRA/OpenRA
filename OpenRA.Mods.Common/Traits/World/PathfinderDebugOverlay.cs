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

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the A* PathDebug from DeveloperMode. Attach this to the world actor.")]
	public class PathfinderDebugOverlayInfo : TraitInfo<PathfinderDebugOverlay> { }
	public class PathfinderDebugOverlay : IRenderOverlay, IWorldLoaded
	{
		Dictionary<Player, CellLayer<int>> layers;
		int refreshTick;
		World world;
		public bool Visible;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			refreshTick = 0;
			layers = new Dictionary<Player, CellLayer<int>>(8);

			// Enabled via Cheats menu
			Visible = false;
		}

		public void AddLayer(IEnumerable<Pair<CPos, int>> cellWeights, int maxWeight, Player pl)
		{
			if (maxWeight == 0) return;

			CellLayer<int> layer;
			if (!layers.TryGetValue(pl, out layer))
			{
				layer = new CellLayer<int>(world.Map);
				layers.Add(pl, layer);
			}

			foreach (var p in cellWeights)
				layer[p.First] = Math.Min(128, layer[p.First] + (maxWeight - p.Second) * 64 / maxWeight);
		}

		public void Render(WorldRenderer wr)
		{
			if (!Visible)
				return;

			var qr = Game.Renderer.WorldRgbaColorRenderer;
			var doDim = refreshTick - world.WorldTick <= 0;
			if (doDim) refreshTick = world.WorldTick + 20;

			var map = wr.World.Map;
			foreach (var pair in layers)
			{
				var c = (pair.Key != null) ? pair.Key.Color.RGB : Color.PaleTurquoise;
				var layer = pair.Value;

				// Only render quads in viewing range:
				foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
				{
					if (layer[uv] <= 0)
						continue;

					var w = Math.Max(0, Math.Min(layer[uv], 128));
					if (doDim)
						layer[uv] = layer[uv] * 5 / 6;

					// TODO: This doesn't make sense for isometric terrain
					var pos = wr.World.Map.CenterOfCell(uv.ToCPos(map));
					var tl = wr.ScreenPxPosition(pos - new WVec(512, 512, 0));
					var br = wr.ScreenPxPosition(pos + new WVec(511, 511, 0));
					qr.FillRect(tl, br, Color.FromArgb(w, c));
				}
			}
		}
	}
}
