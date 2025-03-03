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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.Graphics
{
	public class SonicBlastRenderable : IRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = [];
		readonly SonicBlastRenderer renderer;
		readonly float3 r;

		public WPos Pos { get; }
		public int ZOffset => 0;

		public SonicBlastRenderable(SonicBlastRenderer renderer, WPos pos)
		{
			this.renderer = renderer;
			Pos = pos;
			r = renderer.Info.Size * new float3(0.5f, 0.5f, 0);
		}

		public bool IsDecoration => false;

		public IRenderable WithZOffset(int newOffset) => this;
		public IRenderable OffsetBy(in WVec offset) => this;
		public IRenderable AsDecoration() => this;

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			renderer.Draw(wr.Screen3DPxPosition(Pos));
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var pos = wr.Screen3DPxPosition(Pos);
			var tl = wr.Viewport.WorldToViewPx(pos - r);
			var br = wr.Viewport.WorldToViewPx(pos + r);
			Game.Renderer.RgbaColorRenderer.DrawRect(tl, br, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var pos = wr.Screen3DPxPosition(Pos);
			var tl = wr.Viewport.WorldToViewPx(pos - r);
			var br = wr.Viewport.WorldToViewPx(pos + r);
			return new Rectangle(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
		}
	}
}
