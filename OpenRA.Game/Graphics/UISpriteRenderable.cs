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

using System.Numerics;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class UISpriteRenderable : IRenderable, IPalettedRenderable, IFinalizedRenderable
	{
		readonly Sprite sprite;
		readonly Vector2 screenPos;
		readonly float scale;
		readonly float alpha;
		readonly float rotation = 0f;

		public UISpriteRenderable(Sprite sprite, WPos effectiveWorldPos, Vector2 screenPos, int zOffset, PaletteReference palette,
			float scale = 1f, float alpha = 1f, float rotation = 0f)
		{
			this.sprite = sprite;
			Pos = effectiveWorldPos;
			this.screenPos = screenPos;
			ZOffset = zOffset;
			Palette = palette;
			this.scale = scale;
			this.alpha = alpha;
			this.rotation = rotation;

			// PERF: Remove useless palette assignments for RGBA sprites
			// HACK: This is working around the fact that palettes are defined on traits rather than sequences
			// and can be removed once this has been fixed
			if (sprite.Channel == TextureChannel.RGBA && !(palette?.HasColorShift ?? false))
				Palette = null;
		}

		// Does not exist in the world, so a world positions don't make sense
		public WPos Pos { get; }
		public WVec Offset => WVec.Zero;
		public bool IsDecoration => true;

		public PaletteReference Palette { get; }
		public int ZOffset { get; }

		public IPalettedRenderable WithPalette(PaletteReference newPalette) =>
			new UISpriteRenderable(sprite, Pos, screenPos, ZOffset, newPalette, scale, alpha, rotation);
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(sprite, Palette, screenPos.AsVector3(), scale, Vector3.One, alpha, rotation);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = (screenPos + sprite.Offset.AsVector2()).AsVector3();
			var spriteSize = new Vector3(sprite.Size.X, sprite.Size.Y, 0);
			if (rotation == 0f)
				Game.Renderer.RgbaColorRenderer.DrawRect(offset, offset + spriteSize, 1, Color.Red);
			else
				Game.Renderer.RgbaColorRenderer.DrawPolygon(Util.RotateQuad(offset, spriteSize, rotation), 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var offset = screenPos.AsVector3() + sprite.Offset;
			return Util.BoundingRectangle(offset, sprite.Size, rotation);
		}
	}
}
