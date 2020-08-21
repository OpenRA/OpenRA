#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public struct TargetLineRenderable : IRenderable, IFinalizedRenderable
	{
		readonly IEnumerable<WPos> waypoints;
		readonly Color color;
		readonly int width;
		readonly int markerSize;

		public TargetLineRenderable(IEnumerable<WPos> waypoints, Color color, int width = 1, int markerSize = 1)
		{
			this.waypoints = waypoints;
			this.color = color;
			this.width = width;
			this.markerSize = markerSize;
		}

		public WPos Pos { get { return waypoints.First(); } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new TargetLineRenderable(waypoints, color); }
		public IRenderable OffsetBy(WVec vec) { return new TargetLineRenderable(waypoints.Select(w => w + vec), color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (!waypoints.Any())
				return;

			var first = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(waypoints.First()));
			var a = first;
			foreach (var b in waypoints.Skip(1).Select(pos => wr.Viewport.WorldToViewPx(wr.Screen3DPosition(pos))))
			{
				Game.Renderer.RgbaColorRenderer.DrawLine(a, b, width, color);
				DrawTargetMarker(wr, color, b, markerSize);
				a = b;
			}

			DrawTargetMarker(wr, color, first);
		}

		public static void DrawTargetMarker(WorldRenderer wr, Color color, int2 screenPos, int size = 1)
		{
			var offset = new int2(size, size);
			var tl = screenPos - offset;
			var br = screenPos + offset;
			Game.Renderer.RgbaColorRenderer.FillRect(tl, br, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
