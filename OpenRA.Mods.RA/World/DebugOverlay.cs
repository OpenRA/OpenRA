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
		int[,] layer;
		int refreshTick;
		World world;

		public void WorldLoaded(World w)
		{
			this.world = w;
			this.layer = new int[w.Map.MapSize.X, w.Map.MapSize.Y];
			this.refreshTick = 0;
		}

		public void AddLayer(IEnumerable<Pair<CPos, int>> cellWeights, int maxWeight)
		{
			if (maxWeight == 0) return;

			foreach (var p in cellWeights)
				layer[p.First.X, p.First.Y] = layer[p.First.X, p.First.Y] / 2 + (p.Second * 128 / maxWeight);
		}

		public void Render(WorldRenderer wr)
		{
			var qr = Game.Renderer.WorldQuadRenderer;
			bool doSwap = refreshTick - world.FrameNumber <= 0;
			if (doSwap) refreshTick = world.FrameNumber + 20;

			for (int j = world.Map.Bounds.Top; j <= world.Map.Bounds.Bottom; ++j)
				for (int i = world.Map.Bounds.Left; i <= world.Map.Bounds.Right; ++i)
				{
					if (!world.Map.IsInMap(i, j)) continue;

					var cell = new CPos(i, j);
					var pix = cell.ToPPos();

					var w = Math.Max(0, Math.Min(layer[i, j], 224));
					if (doSwap)
					{
						layer[i, j] = layer[i, j] * 4 / 5;
					}
					qr.FillRect(new RectangleF(pix.X, pix.Y, Game.CellSize, Game.CellSize), Color.FromArgb(w, Color.White));
				}
		}
	}
}
