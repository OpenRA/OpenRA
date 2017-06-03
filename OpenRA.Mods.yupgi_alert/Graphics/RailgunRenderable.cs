#region Copyright & License Information
/*
 * Modded from LaserZap by Boolbada of OP Mod
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Yupgi_alert.Projectiles;

namespace OpenRA.Mods.Yupgi_alert.Graphics
{
	public struct RailgunRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly int zOffset;
		readonly Railgun railgun;
		readonly RailgunInfo info;
		readonly int helixRadius;
		readonly int alpha;
		readonly int ticks;

		WAngle angle;

		// Not-so OOP function signature but this is a game, must save computations!
		public RailgunRenderable(WPos pos, int zOffset, Railgun railgun, RailgunInfo railgunInfo, int ticks)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.railgun = railgun;
			this.info = railgunInfo;
			this.ticks = ticks;

			helixRadius = info.HelixRadius + ticks * info.HelixRadiusDeltaPerTick;
			alpha = 128 - ticks * 8;
			if (alpha < 0)
				alpha = 0;
			angle = new WAngle(ticks * info.HelixAngleDeltaPerTick);
		}

		public WPos Pos { get { return pos; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new RailgunRenderable(pos, zOffset, railgun, info, ticks); }
		public IRenderable WithZOffset(int newOffset) { return new RailgunRenderable(pos, newOffset, railgun, info, ticks); }
		public IRenderable OffsetBy(WVec vec) { return new RailgunRenderable(pos + vec, zOffset, railgun, info, ticks); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (railgun.ForwardStep == WVec.Zero)
				return;

			// Main beam (straight line)
			Game.Renderer.WorldRgbaColorRenderer.DrawLine(
				wr.Screen3DPosition(this.pos),
				wr.Screen3DPosition(this.pos + railgun.SourceToTarget),
				wr.ScreenVector(new WVec(info.BeamThickness, 0, 0))[0],
				railgun.BeamColor);

			int cycleCnt = railgun.SourceToTarget.Length / info.HelixPitch;
			if (railgun.SourceToTarget.Length % info.HelixPitch != 0)
				cycleCnt += 1; // I'm emulating math.ceil

			var screenWidth = wr.ScreenVector(new WVec(info.HelixThichkess, 0, 0))[0];

			// last point the rad beam "reached"
			var pos = this.pos; // where we are.
			float3 last = wr.Screen3DPosition(pos);
			for (var i = cycleCnt * info.QuantizationCount; i > 0; i--)
			{
				// Make it narrower near the end.
				var rad = i < info.QuantizationCount ? helixRadius / 4 :
					i < 2 * info.QuantizationCount ? helixRadius / 2 :
					helixRadius;

				var u = rad * angle.Cos() * railgun.RightVector / (1024 * 1024)
					+ rad * angle.Sin() * railgun.UpVector / (1024 * 1024);
				var end = wr.Screen3DPosition(pos + u);

				Game.Renderer.WorldRgbaColorRenderer.DrawLine(last, end, screenWidth, Color.FromArgb(alpha, railgun.HelixColor));

				pos += railgun.ForwardStep; // keep moving along x axis
				last = end;
				angle += railgun.AngleStep;
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
