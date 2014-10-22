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
using System.Windows.Forms;
using SGraphics = System.Drawing.Graphics;

namespace OpenRA.Editor
{
	class BrushTool : ITool
	{
		BrushTemplate brushTemplate;

		public BrushTool(BrushTemplate brush) { this.brushTemplate = brush; }

		public void Apply(Surface surface)
		{
			// change the bits in the map
			var template = surface.TileSet.Templates[brushTemplate.N];
			var tile = surface.TileSetRenderer.Data(brushTemplate.N);
			var pos = surface.GetBrushLocation();

			if (surface.GetModifiers() == Keys.Shift)
			{
				FloodFillWithBrush(surface, pos);
				return;
			}

			for (var u = 0; u < template.Size.X; u++)
				for (var v = 0; v < template.Size.Y; v++)
				{
					var cell = pos + new CVec(u, v);
					if (surface.Map.Contains(cell))
					{
						var z = u + v * template.Size.X;
						if (tile != null && tile[z].Length > 0)
						{
							var index = template.PickAny ? (byte)((u + pos.X) % 4 + ((v + pos.Y) % 4) * 4) : (byte)z;
							surface.Map.MapTiles.Value[cell] = new TerrainTile(brushTemplate.N, index);
						}

						var ch = new int2((pos.X + u) / Surface.ChunkSize, (pos.Y + v) / Surface.ChunkSize);
						if (surface.Chunks.ContainsKey(ch))
						{
							surface.Chunks[ch].Dispose();
							surface.Chunks.Remove(ch);
						}
					}
				}
		}

		public void Preview(Surface surface, SGraphics g)
		{
			g.DrawImage(brushTemplate.Bitmap,
					surface.TileSetRenderer.TileSize * surface.GetBrushLocation().X * surface.Zoom + surface.GetOffset().X,
					surface.TileSetRenderer.TileSize * surface.GetBrushLocation().Y * surface.Zoom + surface.GetOffset().Y,
					brushTemplate.Bitmap.Width * surface.Zoom,
					brushTemplate.Bitmap.Height * surface.Zoom);
		}

		void FloodFillWithBrush(Surface s, CPos pos)
		{
			var queue = new Queue<CPos>();
			var replace = s.Map.MapTiles.Value[pos];
			var touched = new bool[s.Map.MapSize.X, s.Map.MapSize.Y];

			Action<int, int> maybeEnqueue = (x, y) =>
			{
				var c = new CPos(x, y);
				if (s.Map.Contains(c) && !touched[x, y])
				{
					queue.Enqueue(c);
					touched[x, y] = true;
				}
			};

			queue.Enqueue(pos);
			while (queue.Count > 0)
			{
				var p = queue.Dequeue();
				if (s.Map.MapTiles.Value[p].Type != replace.Type)
					continue;

				var a = FindEdge(s, p, new CVec(-1, 0), replace);
				var b = FindEdge(s, p, new CVec(1, 0), replace);

				for (var x = a.X; x <= b.X; x++)
				{
					s.Map.MapTiles.Value[new CPos(x, p.Y)] = new TerrainTile(brushTemplate.N, (byte)0);
					if (s.Map.MapTiles.Value[new CPos(x, p.Y - 1)].Type == replace.Type)
						maybeEnqueue(x, p.Y - 1);
					if (s.Map.MapTiles.Value[new CPos(x, p.Y + 1)].Type == replace.Type)
						maybeEnqueue(x, p.Y + 1);
				}
			}

			/* TODO: optimize */
			foreach (var ch in s.Chunks.Values) ch.Dispose();
			s.Chunks.Clear();
		}

		static CPos FindEdge(Surface s, CPos p, CVec d, TerrainTile replace)
		{
			for (;;)
			{
				var q = p + d;
				if (!s.Map.Contains(q)) return p;
				if (s.Map.MapTiles.Value[q].Type != replace.Type) return p;
				p = q;
			}
		}
	}
}
