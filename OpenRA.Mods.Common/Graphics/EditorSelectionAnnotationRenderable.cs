#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	/// <summary>
	/// Render the current editor area selection or paste region.
	/// </summary>
	public class EditorSelectionAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly Color color;
		readonly CellCoordsRegion bounds;
		readonly IEnumerable<CPos> cells;
		readonly bool useCells;
		readonly int2 altPixelOffset;
		readonly CVec offset;
		readonly int lineWidth;

		public EditorSelectionAnnotationRenderable(
			CellCoordsRegion bounds, Color color, int2 altPixelOffset, CVec offset, int lineWidth = 1)
		{
			this.bounds = bounds;
			this.color = color;
			this.altPixelOffset = altPixelOffset;
			this.offset = offset;
			this.lineWidth = lineWidth;
			useCells = false;
		}

		public EditorSelectionAnnotationRenderable(
			IEnumerable<CPos> cells, Color color, int2 altPixelOffset, CVec offset, int lineWidth = 1)
		{
			this.cells = cells;
			this.color = color;
			this.altPixelOffset = altPixelOffset;
			this.offset = offset;
			this.lineWidth = lineWidth;
			useCells = true;
		}

		public WPos Pos => WPos.Zero;

		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var map = wr.World.Map;
			foreach (var cellPos in useCells ? cells : bounds)
			{
				var pos = cellPos + offset;
				var uv = pos.ToMPos(map);
				if (!map.Height.Contains(uv))
					continue;

				var ramp = map.Grid.Ramps[map.Ramp[uv]];
				var wPos = map.CenterOfCell(pos) - new WVec(0, 0, ramp.CenterHeightOffset);

				foreach (var p in ramp.Polygons)
				{
					for (var i = 0; i < p.Length; i++)
					{
						var j = (i + 1) % p.Length;
						var start = wPos + p[i];
						var end = wPos + p[j];

						Game.Renderer.RgbaColorRenderer.DrawLine(
							wr.Viewport.WorldToViewPx(wr.ScreenPosition(start)) + altPixelOffset,
							wr.Viewport.WorldToViewPx(wr.Screen3DPosition(end)) + altPixelOffset,
							lineWidth, color, color);
					}
				}
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
