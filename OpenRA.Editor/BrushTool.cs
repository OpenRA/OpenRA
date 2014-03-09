#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
					if (surface.Map.IsInMap(new CVec(u, v) + pos))
					{
						var z = u + v * template.Size.X;
						if (tile[z].Length > 0)
							surface.Map.MapTiles.Value[u + pos.X, v + pos.Y] =
								new TileReference<ushort, byte>
								{
									Type = brushTemplate.N,
									Index = template.PickAny ? (byte)((u + pos.X) % 4 + ((v + pos.Y) % 4) * 4) : (byte)z,
								};

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
			var replace = s.Map.MapTiles.Value[pos.X, pos.Y];
			var touched = new bool[s.Map.MapSize.X, s.Map.MapSize.Y];

			Action<int, int> maybeEnqueue = (x, y) =>
			{
				var c = new CPos(x, y);
				if (s.Map.IsInMap(c) && !touched[x, y])
				{
					queue.Enqueue(c);
					touched[x, y] = true;
				}
			};

			queue.Enqueue(pos);
			while (queue.Count > 0)
			{
				var p = queue.Dequeue();
				if (s.Map.MapTiles.Value[p.X, p.Y].Type != replace.Type)
					continue;

				var a = FindEdge(s, p, new CVec(-1, 0), replace);
				var b = FindEdge(s, p, new CVec(1, 0), replace);

				for (var x = a.X; x <= b.X; x++)
				{
					s.Map.MapTiles.Value[x, p.Y] = new TileReference<ushort, byte> { Type = brushTemplate.N, Index = (byte)0 };
					if (s.Map.MapTiles.Value[x, p.Y - 1].Type == replace.Type)
						maybeEnqueue(x, p.Y - 1);
					if (s.Map.MapTiles.Value[x, p.Y + 1].Type == replace.Type)
						maybeEnqueue(x, p.Y + 1);
				}
			}

			/* TODO: optimize */
			foreach (var ch in s.Chunks.Values) ch.Dispose();
			s.Chunks.Clear();
		}

		static CPos FindEdge(Surface s, CPos p, CVec d, TileReference<ushort, byte> replace)
		{
			for (;;)
			{
				var q = p + d;
				if (!s.Map.IsInMap(q)) return p;
				if (s.Map.MapTiles.Value[q.X, q.Y].Type != replace.Type) return p;
				p = q;
			}
		}
	}
}
