#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class TargetLineRenderable : IRenderable, IFinalizedRenderable
	{
		readonly IEnumerable<WPos> waypoints;
		readonly Color color;
		readonly int width;
		readonly int markerSize;

		public TargetLineRenderable(IEnumerable<WPos> waypoints, Color color, int width, int markerSize)
		{
			this.waypoints = waypoints;
			this.color = color;
			this.width = width;
			this.markerSize = markerSize;
		}

		public WPos Pos => waypoints.First();
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }

		public IRenderable OffsetBy(in WVec vec)
		{
			// Lambdas can't use 'in' variables, so capture a copy for later
			var offset = vec;
			return new TargetLineRenderable(waypoints.Select(w => w + offset), color, width, markerSize);
		}

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
				DrawTargetMarker(color, b, markerSize);
				a = b;
			}

			DrawTargetMarker(color, first);
		}

		public static void DrawTargetMarker(Color color, int2 screenPos, int size = 1)
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
