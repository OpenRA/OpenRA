#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Graphics
{
	public struct SpriteRenderable : IRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = new IRenderable[0].AsEnumerable();

		readonly Sprite sprite;
		readonly WPos pos;
		readonly WVec offset;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;
		readonly bool isDecoration;

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, bool isDecoration)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.offset = offset;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
			this.isDecoration = isDecoration;
		}

		public WPos Pos { get { return pos + offset; } }
		public WVec Offset { get { return offset; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return isDecoration; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new SpriteRenderable(sprite, pos, offset, zOffset, newPalette, scale, isDecoration); }
		public IRenderable WithZOffset(int newOffset) { return new SpriteRenderable(sprite, pos, offset, newOffset, palette, scale, isDecoration); }
		public IRenderable OffsetBy(WVec vec) { return new SpriteRenderable(sprite, pos + vec, offset, zOffset, palette, scale, isDecoration); }
		public IRenderable AsDecoration() { return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, true); }

		float3 ScreenPosition(WorldRenderer wr)
		{
			var xy = wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset) - (0.5f * scale * sprite.Size.XY).ToInt2();

			// HACK: The z offset needs to be applied somewhere, but this probably is the wrong place.
			return new float3(xy, sprite.Offset.Z + wr.ScreenZPosition(pos, 0) - 0.5f * scale * sprite.Size.Z);
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, ScreenPosition(wr), palette, scale * sprite.Size);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = ScreenPosition(wr) + sprite.Offset.XY;
			Game.Renderer.WorldRgbaColorRenderer.DrawRect(offset.XY, (offset + sprite.Size).XY, 1 / wr.Viewport.Zoom, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var offset = ScreenPosition(wr) + sprite.Offset;
			return new Rectangle((int)offset.X, (int)offset.Y, (int)sprite.Size.X, (int)sprite.Size.Y);
		}
	}
}
