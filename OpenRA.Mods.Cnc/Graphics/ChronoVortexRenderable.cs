#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Graphics
{
	public class ChronoVortexRenderable : IRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = [];
		readonly ChronoVortexRenderer renderer;
		public WPos Pos { get; }
		readonly int frame;

		public ChronoVortexRenderable(ChronoVortexRenderer renderer, WPos pos, int frame)
		{
			if (frame < 0 || frame >= 48)
				throw new ArgumentException("frame must be in the range 0-47", nameof(frame));

			this.renderer = renderer;
			Pos = pos;
			this.frame = frame;
		}

		public int ZOffset => 0;
		public bool IsDecoration => false;

		public IRenderable WithZOffset(int newOffset) => this;
		public IRenderable OffsetBy(in WVec offset) => this;
		public IRenderable AsDecoration() => this;

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			renderer.DrawVortex(wr.Screen3DPxPosition(Pos), frame);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var pos = wr.Screen3DPxPosition(Pos);
			var tl = wr.Viewport.WorldToViewPx(pos);
			var br = wr.Viewport.WorldToViewPx(pos + new float3(64, 64, 0));
			Game.Renderer.RgbaColorRenderer.DrawRect(tl, br, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var pos = wr.Screen3DPxPosition(Pos);
			var tl = wr.Viewport.WorldToViewPx(pos);
			var br = wr.Viewport.WorldToViewPx(pos + new float3(64, 64, 0));
			return new Rectangle(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
		}
	}
}
