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
	public struct RangeCircleAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		const int RangeCircleSegments = 32;
		static readonly Int32Matrix4x4[] RangeCircleStartRotations = Exts.MakeArray(RangeCircleSegments, i => WRot.FromFacing(8 * i).AsMatrix());
		static readonly Int32Matrix4x4[] RangeCircleEndRotations = Exts.MakeArray(RangeCircleSegments, i => WRot.FromFacing(8 * i + 6).AsMatrix());

		readonly WPos centerPosition;
		readonly WDist radius;
		readonly int zOffset;
		readonly Color color;
		readonly float width;
		readonly Color borderColor;
		readonly float borderWidth;

		public RangeCircleAnnotationRenderable(WPos centerPosition, WDist radius, int zOffset, Color color, float width, Color borderColor, float borderWidth)
		{
			this.centerPosition = centerPosition;
			this.radius = radius;
			this.zOffset = zOffset;
			this.color = color;
			this.width = width;
			this.borderColor = borderColor;
			this.borderWidth = borderWidth;
		}

		public WPos Pos { get { return centerPosition; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new RangeCircleAnnotationRenderable(centerPosition, radius, newOffset, color, width, borderColor, borderWidth); }
		public IRenderable OffsetBy(WVec vec) { return new RangeCircleAnnotationRenderable(centerPosition + vec, radius, zOffset, color, width, borderColor, borderWidth); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			DrawRangeCircle(wr, centerPosition, radius, width, color, borderWidth, borderColor);
		}

		public static void DrawRangeCircle(WorldRenderer wr, WPos centerPosition, WDist radius,
			float width, Color color, float borderWidth, Color borderColor)
		{
			var cr = Game.Renderer.RgbaColorRenderer;
			var offset = new WVec(radius.Length, 0, 0);
			for (var i = 0; i < RangeCircleSegments; i++)
			{
				var a = wr.Viewport.WorldToViewPx(wr.ScreenPosition(centerPosition + offset.Rotate(ref RangeCircleStartRotations[i])));
				var b = wr.Viewport.WorldToViewPx(wr.ScreenPosition(centerPosition + offset.Rotate(ref RangeCircleEndRotations[i])));

				if (borderWidth > 0)
					cr.DrawLine(a, b, borderWidth, borderColor);

				if (width > 0)
					cr.DrawLine(a, b, width, color);
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
