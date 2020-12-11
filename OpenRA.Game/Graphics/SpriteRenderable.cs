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
	public struct SpriteRenderable : IPalettedRenderable, IModifyableRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = new IRenderable[0];

		readonly Sprite sprite;
		readonly WPos pos;
		readonly WVec offset;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;
		readonly float3 tint;
		readonly TintModifiers tintModifiers;
		readonly float alpha;
		readonly bool isDecoration;

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, bool isDecoration)
			: this(sprite, pos, offset, zOffset, palette, scale, 1f, float3.Ones, TintModifiers.None, isDecoration) { }

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, bool isDecoration, TintModifiers tintModifiers)
			: this(sprite, pos, offset, zOffset, palette, scale, 1f, float3.Ones, tintModifiers, isDecoration) { }

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, float alpha, float3 tint, TintModifiers tintModifiers, bool isDecoration)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.offset = offset;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
			this.tint = tint;
			this.isDecoration = isDecoration;
			this.tintModifiers = tintModifiers;
			this.alpha = alpha;
		}

		public WPos Pos { get { return pos + offset; } }
		public WVec Offset { get { return offset; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return isDecoration; } }

		public float Alpha { get { return alpha; } }
		public float3 Tint { get { return tint; } }
		public TintModifiers TintModifiers { get { return tintModifiers; } }

		public IPalettedRenderable WithPalette(PaletteReference newPalette) { return new SpriteRenderable(sprite, pos, offset, zOffset, newPalette, scale, alpha, tint, tintModifiers, isDecoration); }
		public IRenderable WithZOffset(int newOffset) { return new SpriteRenderable(sprite, pos, offset, newOffset, palette, scale, alpha, tint, tintModifiers, isDecoration); }
		public IRenderable OffsetBy(WVec vec) { return new SpriteRenderable(sprite, pos + vec, offset, zOffset, palette, scale, alpha, tint, tintModifiers, isDecoration); }
		public IRenderable AsDecoration() { return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, alpha, tint, tintModifiers, true); }

		public IModifyableRenderable WithAlpha(float newAlpha)
		{
			return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, newAlpha, tint, tintModifiers, isDecoration);
		}

		public IModifyableRenderable WithTint(in float3 newTint, TintModifiers newTintModifiers)
		{
			return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, alpha, newTint, newTintModifiers, isDecoration);
		}

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
			var t = alpha * tint;
			if (wr.TerrainLighting != null && (tintModifiers & TintModifiers.IgnoreWorldTint) == 0)
				t *= wr.TerrainLighting.TintAt(pos);

			// Shader interprets negative alpha as a flag to use the tint colour directly instead of multiplying the sprite colour
			var a = alpha;
			if ((tintModifiers & TintModifiers.ReplaceColor) != 0)
				a *= -1;

			wsr.DrawSprite(sprite, ScreenPosition(wr), palette, scale * sprite.Size, t, a);
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
