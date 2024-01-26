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

using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class MarkerTileRenderable : IRenderable, IFinalizedRenderable
	{
		readonly CPos pos;
		readonly Color color;

		public MarkerTileRenderable(CPos pos, Color color)
		{
			this.pos = pos;
			this.color = color;
		}

		public WPos Pos => WPos.Zero;
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }

		public IRenderable OffsetBy(in WVec vec)
		{
			return new MarkerTileRenderable(pos, color);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var map = wr.World.Map;
			var r = map.Grid.Ramps[map.Ramp[pos]];
			var wpos = map.CenterOfCell(pos) - new WVec(0, 0, r.CenterHeightOffset);

			var corners = r.Corners.Select(corner => wr.Viewport.WorldToViewPx(wr.Screen3DPosition(wpos + corner))).ToList();

			Game.Renderer.RgbaColorRenderer.FillRect(corners[0], corners[1], corners[2], corners[3], color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
