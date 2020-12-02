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

using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public struct SpriteRenderable : IRenderable, ITintableRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = new IRenderable[0];

		readonly Sprite sprite;
		readonly WPos pos;
		readonly WVec offset;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;
		readonly float3 tint;
		readonly bool isDecoration;
		readonly bool ignoreWorldTint;

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, bool isDecoration)
			: this(sprite, pos, offset, zOffset, palette, scale, float3.Ones, isDecoration, false) { }

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, bool isDecoration, bool ignoreWorldTint)
			: this(sprite, pos, offset, zOffset, palette, scale, float3.Ones, isDecoration, ignoreWorldTint) { }

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, float3 tint, bool isDecoration, bool ignoreWorldTint)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.offset = offset;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
			this.tint = tint;
			this.isDecoration = isDecoration;
			this.ignoreWorldTint = ignoreWorldTint;
		}

		public WPos Pos { get { return pos + offset; } }
		public WVec Offset { get { return offset; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return isDecoration; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new SpriteRenderable(sprite, pos, offset, zOffset, newPalette, scale, tint, isDecoration, ignoreWorldTint); }
		public IRenderable WithZOffset(int newOffset) { return new SpriteRenderable(sprite, pos, offset, newOffset, palette, scale, tint, isDecoration, ignoreWorldTint); }
		public IRenderable OffsetBy(WVec vec) { return new SpriteRenderable(sprite, pos + vec, offset, zOffset, palette, scale, tint, isDecoration, ignoreWorldTint); }
		public IRenderable AsDecoration() { return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, tint, true, ignoreWorldTint); }

		public IRenderable WithTint(in float3 newTint) { return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, newTint, isDecoration, ignoreWorldTint); }

		float3 ScreenPosition(WorldRenderer wr)
		{
			var xy = wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset) - (0.5f * scale * sprite.Size.XY).ToInt2();

			// HACK: The z offset needs to be applied somewhere, but this probably is the wrong place.
			return new float3(xy, sprite.Offset.Z + wr.ScreenZPosition(pos, 0) - 0.5f * scale * sprite.Size.Z);
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var wsr = Game.Renderer.WorldSpriteRenderer;
			if (ignoreWorldTint)
				wsr.DrawSprite(sprite, ScreenPosition(wr), palette, scale * sprite.Size);
			else
			{
				var t = tint;
				if (wr.TerrainLighting != null)
					t *= wr.TerrainLighting.TintAt(pos);

				wsr.DrawSpriteWithTint(sprite, ScreenPosition(wr), palette, scale * sprite.Size, t);
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var pos = ScreenPosition(wr) + sprite.Offset;
			var tl = wr.Viewport.WorldToViewPx(pos);
			var br = wr.Viewport.WorldToViewPx(pos + sprite.Size);
			Game.Renderer.RgbaColorRenderer.DrawRect(tl, br, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var screenOffset = ScreenPosition(wr) + sprite.Offset;
			return new Rectangle((int)screenOffset.X, (int)screenOffset.Y, (int)sprite.Size.X, (int)sprite.Size.Y);
		}
	}
}
