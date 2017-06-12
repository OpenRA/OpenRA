#region Copyright & License Information
/*
 * Modded from TargetLineRenderable (but nothing like it haha)
 * Modded by Boolbada of OP Mod
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;

/* Works without base engine modification */

namespace OpenRA.Yupgi_alert.Graphics
{
	public struct ArcRenderable : IRenderable, IFinalizedRenderable
	{
		readonly Color color;
		readonly WPos a, b;
		readonly WAngle angle;
		readonly int segments;

		public ArcRenderable(WPos a, WPos b, WAngle angle, Color color, int segments)
		{
			this.a = a;
			this.b = b;
			this.angle = angle;
			this.color = color;
			this.segments = segments;
		}

		public WPos Pos { get { return a; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new ArcRenderable(a, b, angle, color, segments); }
		public IRenderable WithZOffset(int newOffset) { return new ArcRenderable(a, b, angle, color, segments); }
		public IRenderable OffsetBy(WVec vec) { return new ArcRenderable(a + vec, b + vec, angle, color, segments); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var iz = 1 / wr.Viewport.Zoom;

			float3[] points = new float3[segments + 1];
			for (int i = 0; i <= segments; i++)
				points[i] = wr.Screen3DPosition(WPos.LerpQuadratic(a, b, angle, i, segments));

			Game.Renderer.WorldRgbaColorRenderer.DrawLine(points, iz, color, false);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
