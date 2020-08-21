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

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public enum BeamRenderableShape { Cylindrical, Flat }
	public struct BeamRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly int zOffset;
		readonly WVec length;
		readonly BeamRenderableShape shape;
		readonly WDist width;
		readonly Color color;

		public BeamRenderable(WPos pos, int zOffset, WVec length, BeamRenderableShape shape, WDist width, Color color)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.length = length;
			this.shape = shape;
			this.width = width;
			this.color = color;
		}

		public WPos Pos { get { return pos; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new BeamRenderable(pos, zOffset, length, shape, width, color); }
		public IRenderable OffsetBy(WVec vec) { return new BeamRenderable(pos + vec, zOffset, length, shape, width, color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var vecLength = length.Length;
			if (vecLength == 0)
				return;

			if (shape == BeamRenderableShape.Flat)
			{
				var delta = length * width.Length / (2 * vecLength);
				var corner = new WVec(-delta.Y, delta.X, delta.Z);
				var a = wr.Screen3DPosition(pos - corner);
				var b = wr.Screen3DPosition(pos + corner);
				var c = wr.Screen3DPosition(pos + corner + length);
				var d = wr.Screen3DPosition(pos - corner + length);
				Game.Renderer.WorldRgbaColorRenderer.FillRect(a, b, c, d, color);
			}
			else
			{
				var start = wr.Screen3DPosition(pos);
				var end = wr.Screen3DPosition(pos + length);
				var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];
				Game.Renderer.WorldRgbaColorRenderer.DrawLine(start, end, screenWidth, color);
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
