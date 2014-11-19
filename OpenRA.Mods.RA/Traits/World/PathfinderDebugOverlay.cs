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
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Required for the A* PathDebug from DeveloperMode. Attach this to the world actor.")]
	class PathfinderDebugOverlayInfo : TraitInfo<PathfinderDebugOverlay> { }
	class PathfinderDebugOverlay : IRenderOverlay, IWorldLoaded
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

			var qr = Game.Renderer.WorldQuadRenderer;
			var doDim = refreshTick - world.WorldTick <= 0;
			if (doDim) refreshTick = world.WorldTick + 20;

			foreach (var pair in layers)
			{
				var c = (pair.Key != null) ? pair.Key.Color.RGB : Color.PaleTurquoise;
				var layer = pair.Value;

				// Only render quads in viewing range:
				foreach (var cell in wr.Viewport.VisibleCells)
				{
					if (layer[cell] <= 0)
						continue;

					var w = Math.Max(0, Math.Min(layer[cell], 128));
					if (doDim)
						layer[cell] = layer[cell] * 5 / 6;

					// TODO: This doesn't make sense for isometric terrain
					var pos = wr.world.Map.CenterOfCell(cell);
					var tl = wr.ScreenPxPosition(pos - new WVec(512, 512, 0));
					var br = wr.ScreenPxPosition(pos + new WVec(511, 511, 0));
					qr.FillRect(RectangleF.FromLTRB(tl.X, tl.Y, br.X, br.Y), Color.FromArgb(w, c));
				}
			}
		}
	}
}
