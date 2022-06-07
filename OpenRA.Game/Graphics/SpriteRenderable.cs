#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SpriteRenderable : IPalettedRenderable, IModifyableRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = Array.Empty<IRenderable>();

		readonly Sprite sprite;
		readonly WPos pos;
		readonly WVec offset;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;
		readonly WAngle rotation = WAngle.Zero;
		readonly float3 tint;
		readonly TintModifiers tintModifiers;
		readonly float alpha;
		readonly bool isDecoration;

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, float alpha,
			float3 tint, TintModifiers tintModifiers, bool isDecoration, WAngle rotation)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.offset = offset;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
			this.rotation = rotation;
			this.tint = tint;
			this.isDecoration = isDecoration;
			this.tintModifiers = tintModifiers;
			this.alpha = alpha;

			// PERF: Remove useless palette assignments for RGBA sprites
			// HACK: This is working around the fact that palettes are defined on traits rather than sequences
			// and can be removed once this has been fixed
			if (sprite.Channel == TextureChannel.RGBA && !(palette?.HasColorShift ?? false))
				this.palette = null;
		}

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, float alpha,
			float3 tint, TintModifiers tintModifiers, bool isDecoration)
			: this(sprite, pos, offset, zOffset, palette, scale, alpha, tint, tintModifiers, isDecoration, WAngle.Zero) { }

		public WPos Pos => pos + offset;
		public WVec Offset => offset;
		public PaletteReference Palette => palette;
		public int ZOffset => zOffset;
		public bool IsDecoration => isDecoration;

		public float Alpha => alpha;
		public float3 Tint => tint;
		public TintModifiers TintModifiers => tintModifiers;

		public IPalettedRenderable WithPalette(PaletteReference newPalette)
		{
			return new SpriteRenderable(sprite, pos, offset, zOffset, newPalette, scale, alpha, tint, tintModifiers, isDecoration, rotation);
		}

		public IRenderable WithZOffset(int newOffset)
		{
			return new SpriteRenderable(sprite, pos, offset, newOffset, palette, scale, alpha, tint, tintModifiers, isDecoration, rotation);
		}

		public IRenderable OffsetBy(in WVec vec)
		{
			return new SpriteRenderable(sprite, pos + vec, offset, zOffset, palette, scale, alpha, tint, tintModifiers, isDecoration, rotation);
		}

		public IRenderable AsDecoration()
		{
			return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, alpha, tint, tintModifiers, true, rotation);
		}

		public IModifyableRenderable WithAlpha(float newAlpha)
		{
			return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, newAlpha, tint, tintModifiers, isDecoration, rotation);
		}

		public IModifyableRenderable WithTint(in float3 newTint, TintModifiers newTintModifiers)
		{
			return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, alpha, newTint, newTintModifiers, isDecoration, rotation);
		}

		float3 ScreenPosition(WorldRenderer wr)
		{
			var s = 0.5f * scale * sprite.Size;
			return wr.Screen3DPxPosition(pos) + wr.ScreenPxOffset(offset) - new float3((int)s.X, (int)s.Y, s.Z);
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

			wsr.DrawSprite(sprite, palette, ScreenPosition(wr), scale, t, a, rotation.RendererRadians());
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var pos = ScreenPosition(wr) + sprite.Offset;
			var tl = wr.Viewport.WorldToViewPx(pos);
			var br = wr.Viewport.WorldToViewPx(pos + sprite.Size);
			if (rotation == WAngle.Zero)
				Game.Renderer.RgbaColorRenderer.DrawRect(tl, br, 1, Color.Red);
			else
				Game.Renderer.RgbaColorRenderer.DrawPolygon(Util.RotateQuad(tl, br - tl, rotation.RendererRadians()), 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var screenOffset = ScreenPosition(wr) + sprite.Offset;
			return Util.BoundingRectangle(screenOffset, sprite.Size, rotation.RendererRadians());
		}
	}
}
