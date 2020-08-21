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

using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public struct UISpriteRenderable : IRenderable, IPalettedRenderable, IFinalizedRenderable
	{
		readonly Sprite sprite;
		readonly WPos effectiveWorldPos;
		readonly int2 screenPos;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;

		public UISpriteRenderable(Sprite sprite, WPos effectiveWorldPos, int2 screenPos, int zOffset, PaletteReference palette, float scale)
		{
			this.sprite = sprite;
			this.effectiveWorldPos = effectiveWorldPos;
			this.screenPos = screenPos;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
		}

		// Does not exist in the world, so a world positions don't make sense
		public WPos Pos { get { return effectiveWorldPos; } }
		public WVec Offset { get { return WVec.Zero; } }
		public bool IsDecoration { get { return true; } }

		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }

		public IPalettedRenderable WithPalette(PaletteReference newPalette) { return new UISpriteRenderable(sprite, effectiveWorldPos, screenPos, zOffset, newPalette, scale); }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(sprite, screenPos, palette, scale * sprite.Size);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = screenPos + sprite.Offset.XY;
			Game.Renderer.RgbaColorRenderer.DrawRect(offset, offset + sprite.Size.XY, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var offset = screenPos + sprite.Offset;
			return new Rectangle((int)offset.X, (int)offset.Y, (int)sprite.Size.X, (int)sprite.Size.Y);
		}
	}
}
