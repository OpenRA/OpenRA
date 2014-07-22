#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	public struct TargetLineRenderable : IRenderable
	{
		readonly IEnumerable<WPos> waypoints;
		readonly Color color;

		public TargetLineRenderable(IEnumerable<WPos> waypoints, Color color)
		{
			this.waypoints = waypoints;
			this.color = color;
		}

		public WPos Pos { get { return waypoints.First(); } }
		public float Scale { get { return 1f; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithScale(float newScale) { return new TargetLineRenderable(waypoints, color); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new TargetLineRenderable(waypoints, color); }
		public IRenderable WithZOffset(int newOffset) { return new TargetLineRenderable(waypoints, color); }
		public IRenderable OffsetBy(WVec vec) { return new TargetLineRenderable(waypoints.Select(w => w + vec), color); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			if (!waypoints.Any())
				return;

			var first = wr.ScreenPxPosition(waypoints.First());
			var a = first;
			foreach (var b in waypoints.Skip(1).Select(pos => wr.ScreenPxPosition(pos)))
			{
				Game.Renderer.WorldLineRenderer.DrawLine(a, b, color, color);
				wr.DrawTargetMarker(color, b);
				a = b;
			}

			wr.DrawTargetMarker(color, first);
		}

		public void RenderDebugGeometry(WorldRenderer wr) {}
	}
}
