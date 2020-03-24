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
	public struct DetectionCircleAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos centerPosition;
		readonly WDist radius;
		readonly int zOffset;
		readonly int trailCount;
		readonly WAngle trailSeparation;
		readonly WAngle trailAngle;
		readonly Color color;
		readonly Color contrastColor;

		public DetectionCircleAnnotationRenderable(WPos centerPosition, WDist radius, int zOffset,
			int lineTrails, WAngle trailSeparation, WAngle trailAngle, Color color, Color contrastColor)
		{
			this.centerPosition = centerPosition;
			this.radius = radius;
			this.zOffset = zOffset;
			trailCount = lineTrails;
			this.trailSeparation = trailSeparation;
			this.trailAngle = trailAngle;
			this.color = color;
			this.contrastColor = contrastColor;
		}

		public WPos Pos { get { return centerPosition; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette)
		{
			return new DetectionCircleAnnotationRenderable(centerPosition, radius, zOffset,
				trailCount, trailSeparation, trailAngle, color, contrastColor);
		}

		public IRenderable WithZOffset(int newOffset)
		{
			return new DetectionCircleAnnotationRenderable(centerPosition, radius, newOffset,
				trailCount, trailSeparation, trailAngle, color, contrastColor);
		}

		public IRenderable OffsetBy(WVec vec)
		{
			return new DetectionCircleAnnotationRenderable(centerPosition + vec, radius, zOffset,
				trailCount, trailSeparation, trailAngle, color, contrastColor);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var cr = Game.Renderer.RgbaColorRenderer;
			var center = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(centerPosition));

			for (var i = 0; i < trailCount; i++)
			{
				var angle = trailAngle - new WAngle(i * (trailSeparation.Angle <= 512 ? 1 : -1));
				var length = radius.Length * new WVec(angle.Cos(), angle.Sin(), 0) / 1024;
				var end = wr.Viewport.WorldToViewPx(wr.Screen3DPosition(centerPosition + length));
				var alpha = color.A - i * color.A / trailCount;
				cr.DrawLine(center, end, 3, Color.FromArgb(alpha, contrastColor));
				cr.DrawLine(center, end, 1, Color.FromArgb(alpha, color));
			}

			RangeCircleAnnotationRenderable.DrawRangeCircle(wr, centerPosition, radius, 1, color, 3, contrastColor);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
