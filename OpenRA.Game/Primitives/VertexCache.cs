#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Primitives
{
	public sealed class VertexCache
	{
		readonly HashSet<MPos> valid = new HashSet<MPos>();
		readonly Map map;
		readonly int mapWidth;
		readonly Vertex[] vertices;

		public VertexCache(Map map)
		{
			this.map = map;
			var size = map.MapSize;
			mapWidth = size.X;
			vertices = new Vertex[4 * size.X * size.Y];
		}

		public void Invalidate(CPos cell)
		{
			Invalidate(cell.ToMPos(map));
		}

		public void Invalidate(IEnumerable<CPos> cells)
		{
			Invalidate(cells.Select(cell => cell.ToMPos(map)));
		}

		public void Invalidate(MPos uv)
		{
			valid.Remove(uv);
		}

		public void Invalidate(IEnumerable<MPos> uvs)
		{
			valid.ExceptWith(uvs);
		}

		int ArrayOffset(MPos uv)
		{
			return uv.V * mapWidth + uv.U;
		}

		int VertexArrayOffset(MPos uv)
		{
			return 4 * ArrayOffset(uv);
		}

		public void RenderCenteredOverCell(WorldRenderer wr, Sprite sprite, PaletteReference pal, MPos uv)
		{
			var offset = VertexArrayOffset(uv);
			if (valid.Add(uv))
			{
				// TODO: Need to account for ZOffset (-511)
				var location = wr.ScreenPosition(map.CenterOfCell(uv.ToCPos(map))) - 0.5f * sprite.Size;
				Util.FastCreateQuad(vertices, location, sprite, pal, offset);
			}

			Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, vertices, offset);
		}
	}
}
