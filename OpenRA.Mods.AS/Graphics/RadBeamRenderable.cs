#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Mods.AS.Graphics
{
	public struct RadBeamRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly int zOffset;
		readonly WVec length;
		readonly WDist width;
		readonly Color color;
		readonly WDist amplitude;
		readonly WDist wavelength;

		// integer version of sine wave (LUT).
		// 100 * ( i * pi / 16 ).
		static readonly int[] SineLUT = { 0, 19, 38, 55, 71, 83, 92, 98, 100, 98, 92, 83, 71, 55, 38, 19,
										 0, -19, -38, -55, -71, -83, -92, -98, -100, -98, -92, -83, -71, -55, -38, -19 };

		public RadBeamRenderable(WPos pos, int zOffset, WVec length, WDist width, Color color, WDist amplitude, WDist wavelength)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.length = length;
			this.width = width;
			this.color = color;
			this.amplitude = amplitude;
			this.wavelength = wavelength;
		}

		public WPos Pos { get { return pos; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new RadBeamRenderable(pos, zOffset, length, width, color, amplitude, wavelength); }
		public IRenderable WithZOffset(int newOffset) { return new RadBeamRenderable(pos, zOffset, length, width, color, amplitude, wavelength); }
		public IRenderable OffsetBy(WVec vec) { return new RadBeamRenderable(pos + vec, zOffset, length, width, color, amplitude, wavelength); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var vecLength = length.Length;
			if (vecLength == 0)
				return;

			// Let's compute through x-axis vector and y-axis vector.
			// Fortunately, length is already in x-axis direction. All we need to do is to compute length.
			var x = (length * wavelength.Length) / length.Length;

			// y-axis can be gotten by world vector?? (perpendicular to the ground)
			var y = new WVec(0, 0, amplitude.Length);

			var start = wr.Screen3DPosition(pos);

			int cnt = vecLength / wavelength.Length;
			if (length.Length % wavelength.Length != 0)
				cnt += 1; // I'm emulating math.ceil

			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];

			var last = start; // last point the rad beam "reached"
			var xx = pos;
			for (var i = 0; i < cnt; i++)
			{
				var index = i % SineLUT.Length;
				var sin = y * SineLUT[index] / 100; // value * y vector
				xx += x; // keep moving along x axis
				var end = wr.Screen3DPosition(xx + sin);
				Game.Renderer.WorldRgbaColorRenderer.DrawLine(last, end, screenWidth, color);
				last = end;
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
