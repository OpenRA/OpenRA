#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PathfinderDebugOverlayInfo : TraitInfo<PathfinderDebugOverlay> { }
	class PathfinderDebugOverlay : IRenderOverlay, IWorldLoaded
	{
		Dictionary<Player, int[,]> layers;
		int refreshTick;
		World world;
		public bool Visible;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			refreshTick = 0;
			layers = new Dictionary<Player, int[,]>(8);

			// Enabled via Cheats menu
			Visible = false;
		}

		public void AddLayer(IEnumerable<Pair<CPos, int>> cellWeights, int maxWeight, Player pl)
		{
			if (maxWeight == 0) return;

			int[,] layer;
			if (!layers.TryGetValue(pl, out layer))
			{
				layer = new int[world.Map.MapSize.X, world.Map.MapSize.Y];
				layers.Add(pl, layer);
			}

			foreach (var p in cellWeights)
				layer[p.First.X, p.First.Y] = Math.Min(128, layer[p.First.X, p.First.Y] + (maxWeight - p.Second) * 64 / maxWeight);
		}

		public void Render(WorldRenderer wr)
		{
			if (!Visible)
				return;

			var qr = Game.Renderer.WorldQuadRenderer;
			var doDim = refreshTick - world.WorldTick <= 0;
			if (doDim) refreshTick = world.WorldTick + 20;

			var viewBounds = wr.Viewport.CellBounds;
			foreach (var pair in layers)
			{
				var c = (pair.Key != null) ? pair.Key.Color.RGB : Color.PaleTurquoise;
				var layer = pair.Value;

				// Only render quads in viewing range:
				for (var j = viewBounds.Top; j <= viewBounds.Bottom; ++j)
				{
					for (var i = viewBounds.Left; i <= viewBounds.Right; ++i)
					{
						if (layer[i, j] <= 0)
							continue;

						var w = Math.Max(0, Math.Min(layer[i, j], 128));
						if (doDim)
							layer[i, j] = layer[i, j] * 5 / 6;

						// TODO: This doesn't make sense for isometric terrain
						var tl = wr.ScreenPxPosition(new CPos(i, j).TopLeft);
						var br = wr.ScreenPxPosition(new CPos(i, j).BottomRight);
						qr.FillRect(RectangleF.FromLTRB(tl.X, tl.Y, br.X, br.Y), Color.FromArgb(w, c));
					}
				}
			}
		}
	}
}
