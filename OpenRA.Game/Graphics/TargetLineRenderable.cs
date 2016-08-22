#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Graphics
{
	public struct TargetLineRenderable : IRenderable, IFinalizedRenderable
	{
		readonly IEnumerable<WPos> waypoints;
		readonly Color color;

		public TargetLineRenderable(IEnumerable<WPos> waypoints, Color color)
		{
			this.waypoints = waypoints;
			this.color = color;
		}

		public WPos Pos { get { return waypoints.First(); } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new TargetLineRenderable(waypoints, color); }
		public IRenderable WithZOffset(int newOffset) { return new TargetLineRenderable(waypoints, color); }
		public IRenderable OffsetBy(WVec vec) { return new TargetLineRenderable(waypoints.Select(w => w + vec), color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (!waypoints.Any())
				return;

			var iz = 1 / wr.Viewport.Zoom;
			var first = wr.Screen3DPosition(waypoints.First());
			var a = first;
			foreach (var b in waypoints.Skip(1).Select(pos => wr.Screen3DPosition(pos)))
			{
				Game.Renderer.WorldRgbaColorRenderer.DrawLine(a, b, iz, color);
				DrawTargetMarker(wr, color, b);
				a = b;
			}

			DrawTargetMarker(wr, color, first);
		}

		public static void DrawTargetMarker(WorldRenderer wr, Color color, float3 location)
		{
			var iz = 1 / wr.Viewport.Zoom;
			var offset = new float2(iz, iz);
			var tl = location - offset;
			var br = location + offset;
			Game.Renderer.WorldRgbaColorRenderer.FillRect(tl, br, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
