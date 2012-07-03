using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class DebugOverlayInfo : Traits.TraitInfo<DebugOverlay>
	{
	}

	class DebugOverlay : IRenderOverlay, IWorldLoaded
	{
		Dictionary<Player, int[,]> layers;
		int refreshTick;
		World world;
		public bool Visible;

		public void WorldLoaded(World w)
		{
			this.world = w;
			this.refreshTick = 0;
			this.layers = new Dictionary<Player, int[,]>(8);
			// Enabled via Cheats menu
			this.Visible = false;
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
				layer[p.First.X, p.First.Y] = Math.Min(128, (layer[p.First.X, p.First.Y] / 2) + ((maxWeight - p.Second) * 64 / maxWeight));
		}

		public void Render(WorldRenderer wr)
		{
			if (!Visible) return;

			var qr = Game.Renderer.WorldQuadRenderer;
			bool doDim = refreshTick - world.FrameNumber <= 0;
			if (doDim) refreshTick = world.FrameNumber + 15;

			foreach (var pair in layers)
			{
				Color c = (pair.Key != null) ? pair.Key.ColorRamp.GetColor(0f) : Color.PaleTurquoise;
				var layer = pair.Value;

				for (int j = world.Map.Bounds.Top; j <= world.Map.Bounds.Bottom; ++j)
					for (int i = world.Map.Bounds.Left; i <= world.Map.Bounds.Right; ++i)
					{
						var ploc = new CPos(i, j).ToPPos();

						var w = Math.Max(0, Math.Min(layer[i, j], 128));
						if (doDim)
						{
							layer[i, j] = layer[i, j] * 4 / 5;
						}
						qr.FillRect(new RectangleF(ploc.X, ploc.Y, Game.CellSize, Game.CellSize), Color.FromArgb(w, c));
					}
			}
		}
	}
}
