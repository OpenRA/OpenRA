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

using System;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public readonly struct BorderedRegionRenderable : IRenderable, IFinalizedRenderable
	{
		enum Corner { TopLeft, TopRight, BottomRight, BottomLeft }

		// Maps a cell offset to the index of the corner (in the 'Corner' arrays in the MapGrid.CellRamp structs)
		// from which a border should be drawn. The index of the end corner will be (cornerIndex + 1) % 4.
		static readonly ImmutableArray<(CVec Offset, int CornerIndex)> Offset2CornerIndex =
		[
			(new CVec(0, -1), (int)Corner.TopLeft),
			(new CVec(1, 0), (int)Corner.TopRight),
			(new CVec(0, 1), (int)Corner.BottomRight),
			(new CVec(-1, 0), (int)Corner.BottomLeft),
		];

		readonly CPos[] region;
		readonly Color color, contrastColor;
		readonly float width, contrastWidth;
		readonly bool dashed;

		public BorderedRegionRenderable(CPos[] region, Color color, float width, Color contrastColor, float contrastWidth, bool dashed = false)
		{
			this.region = region;
			this.color = color;
			this.contrastColor = contrastColor;
			this.width = width;
			this.contrastWidth = contrastWidth;
			this.dashed = dashed;
		}

		readonly WPos IRenderable.Pos { get { return WPos.Zero; } }
		readonly int IRenderable.ZOffset { get { return 0; } }
		readonly bool IRenderable.IsDecoration { get { return true; } }

		IRenderable IRenderable.WithZOffset(int newOffset) { return new BorderedRegionRenderable(region, color, width, contrastColor, contrastWidth, dashed); }
		IRenderable IRenderable.OffsetBy(in WVec offset) { return new BorderedRegionRenderable(region, color, width, contrastColor, contrastWidth, dashed); }
		IRenderable IRenderable.AsDecoration() { return this; }

		IFinalizedRenderable IRenderable.PrepareRender(WorldRenderer wr) { return this; }
		void IFinalizedRenderable.Render(WorldRenderer wr) { Draw(wr, region, color, width, contrastColor, contrastWidth, dashed); }
		void IFinalizedRenderable.RenderDebugGeometry(WorldRenderer wr) { }
		Rectangle IFinalizedRenderable.ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }

		public static void Draw(WorldRenderer wr, CPos[] region, Color color, float width, Color constrastColor, float constrastWidth, bool dashed = false)
		{
			if (width == 0 && constrastWidth == 0)
				return;

			var map = wr.World.Map;
			var cr = Game.Renderer.RgbaColorRenderer;
			var regionSet = region.ToHashSet();

			foreach (var c in region)
			{
				var mpos = c.ToMPos(map);
				if (!map.Height.Contains(mpos) || wr.World.ShroudObscures(c))
					continue;

				var tile = map.Tiles[mpos];
				var ti = map.Rules.TerrainInfo.GetTerrainInfo(tile);
				var ramp = ti?.RampType ?? 0;

				var corners = map.Grid.Ramps[ramp].Corners;
				var pos = map.CenterOfCell(c) - new WVec(0, 0, map.Grid.Ramps[ramp].CenterHeightOffset);

				foreach (var (offset, cornerIndex) in Offset2CornerIndex)
				{
					// If the neighboring cell is part of the region, don't draw a border between the cells.
					if (regionSet.Contains(c + offset))
						continue;

					var start = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(pos + corners[cornerIndex]));
					var end = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(pos + corners[(cornerIndex + 1) % 4]));

					if (constrastWidth > 0)
						cr.DrawLine(start, end, constrastWidth, constrastColor);

					if (width > 0)
					{
						if (dashed)
							DrawDashedLine(cr, start, end, width, color);
						else
							cr.DrawLine(start, end, width, color);
					}
				}
			}
		}

		static void DrawDashedLine(RgbaColorRenderer cr, in float3 start, in float3 end, float width, Color color, float dashLength = 5f)
		{
			var delta = end - start;
			var length = delta.XY.Length;
			if (length < 1f)
				return;

			var direction = delta / length;
			var position = 0f;
			var draw = true;
			while (position < length)
			{
				var segmentLength = Math.Min(dashLength, length - position);
				if (draw)
				{
					var segmentStart = start + position * direction;
					var segmentEnd = start + (position + segmentLength) * direction;
					cr.DrawLine(segmentStart, segmentEnd, width, color);
				}

				position += segmentLength;
				draw = !draw;
			}
		}
	}
}
