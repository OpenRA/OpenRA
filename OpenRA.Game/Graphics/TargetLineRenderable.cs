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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

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

			var first = wr.ScreenPxPosition(waypoints.First());
			var a = first;
			foreach (var b in waypoints.Skip(1).Select(pos => wr.ScreenPxPosition(pos)))
			{
				Game.Renderer.WorldLineRenderer.DrawLine(a, b, color);
				DrawTargetMarker(wr, color, b);
				a = b;
			}

			DrawTargetMarker(wr, color, first);
		}

		public static void DrawTargetMarker(WorldRenderer wr, Color c, float2 location)
		{
			var miz = -1 / wr.Viewport.Zoom;
			var tl = new float2(miz, miz);
			var br = -tl;
			var bl = new float2(tl.X, br.Y);
			var tr = new float2(br.X, tl.Y);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(location + tl, location + tr, c);
			wlr.DrawLine(location + tr, location + br, c);
			wlr.DrawLine(location + br, location + bl, c);
			wlr.DrawLine(location + bl, location + tl, c);
		}


		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
