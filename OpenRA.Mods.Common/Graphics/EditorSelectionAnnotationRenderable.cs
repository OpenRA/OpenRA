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
		readonly CellRegion bounds;
		readonly int2 altPixelOffset;
		readonly CPos? offset;

		public EditorSelectionAnnotationRenderable(CellRegion bounds, Color color, int2 altPixelOffset, CPos? offset)
		{
			this.bounds = bounds;
			this.color = color;
			this.altPixelOffset = altPixelOffset;
			this.offset = offset;
		}

		public WPos Pos => WPos.Zero;

		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return new EditorSelectionAnnotationRenderable(bounds, color, new int2(vec.X, vec.Y), offset); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (bounds == null)
				return;

			const int Width = 1;
			var map = wr.World.Map;
			var originalWPos = map.CenterOfCell(bounds.TopLeft);
			var wposOffset = offset.HasValue ? map.CenterOfCell(offset.Value) - originalWPos : WVec.Zero;

			foreach (var cellPos in bounds.CellCoords)
			{
				var uv = cellPos.ToMPos(map);
				if (!map.Height.Contains(uv))
					continue;

				var ramp = map.Grid.Ramps[map.Ramp[uv]];
				var pos = map.CenterOfCell(cellPos) - new WVec(0, 0, ramp.CenterHeightOffset);

				foreach (var p in ramp.Polygons)
				{
					for (var i = 0; i < p.Length; i++)
					{
						var j = (i + 1) % p.Length;
						var start = pos + p[i];
						var end = pos + p[j];

						Game.Renderer.RgbaColorRenderer.DrawLine(
							wr.Viewport.WorldToViewPx(wr.ScreenPosition(start + wposOffset)) + altPixelOffset,
							wr.Viewport.WorldToViewPx(wr.Screen3DPosition(end + wposOffset)) + altPixelOffset,
							Width, color, color);
					}
				}
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
